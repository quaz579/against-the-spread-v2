import { BlobServiceClient } from '@azure/storage-blob';
import { ChildProcess, spawn } from 'child_process';
import * as fs from 'fs';
import * as http from 'http';
import * as path from 'path';

/**
 * Test environment manager for Playwright smoke tests
 * Manages Azurite, Azure Functions, and Blazor Web App processes
 */
export class TestEnvironment {
  private azuriteProcess?: ChildProcess;
  private functionsProcess?: ChildProcess;
  private webProcess?: ChildProcess;

  private readonly repoRoot: string;
  private readonly azuriteConnectionString =
    'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;';

  public readonly functionsUrl = 'http://localhost:7071';
  public readonly webUrl = 'http://localhost:5158';

  constructor(repoRoot: string) {
    this.repoRoot = repoRoot;
  }

  /**
   * Start Azurite storage emulator
   */
  async startAzurite(): Promise<void> {
    console.log('Starting Azurite...');

    const azuriteDir = path.join('/tmp', 'azurite-test');
    if (!fs.existsSync(azuriteDir)) {
      fs.mkdirSync(azuriteDir, { recursive: true });
    }

    this.azuriteProcess = spawn('npx', [
      'azurite',
      '--location', azuriteDir,
      '--blobPort', '10000',
      '--blobHost', '127.0.0.1'
    ]);

    if (!this.azuriteProcess) {
      throw new Error('Failed to start Azurite');
    }

    // Track when Azurite is ready
    let azuriteReady = false;
    const azuriteReadyPromise = new Promise<void>((resolve) => {
      // Attach output and error handlers for Azurite process
      this.azuriteProcess!.stdout?.on('data', (data) => {
        const output = data.toString().trim();
        console.log(`[Azurite] ${output}`);
        // Azurite outputs "Azurite Blob service is successfully listening" when ready
        if (output.includes('successfully listening') || output.includes('Blob service is successfully')) {
          azuriteReady = true;
          resolve();
        }
      });
      this.azuriteProcess!.stderr?.on('data', (data) => {
        console.error(`[Azurite Error] ${data.toString().trim()}`);
      });
      this.azuriteProcess!.on('error', (error) => {
        console.error('Azurite process error:', error);
      });
    });

    // Wait for Azurite to be ready (either from output or timeout)
    await Promise.race([
      azuriteReadyPromise,
      new Promise(resolve => setTimeout(resolve, 10000)) // 10 second max wait
    ]);

    // Give Azurite a moment to fully initialize
    await new Promise(resolve => setTimeout(resolve, 2000));

    console.log('Azurite started successfully');
  }

  /**
   * Start Azure Functions
   */
  async startFunctions(): Promise<void> {
    console.log('Starting Azure Functions...');

    const functionsPath = path.join(this.repoRoot, 'src', 'AgainstTheSpread.Functions');

    // Create local.settings.json if it doesn't exist
    const localSettingsPath = path.join(functionsPath, 'local.settings.json');
    if (!fs.existsSync(localSettingsPath)) {
      const localSettings = {
        IsEncrypted: false,
        Values: {
          AzureWebJobsStorage: this.azuriteConnectionString,
          AZURE_STORAGE_CONNECTION_STRING: this.azuriteConnectionString,
          FUNCTIONS_WORKER_RUNTIME: "dotnet-isolated"
        },
        Host: {
          CORS: "*"
        }
      };
      fs.writeFileSync(localSettingsPath, JSON.stringify(localSettings, null, 2));
      console.log('Created local.settings.json for Functions with CORS enabled');
    }

    // Build first
    await this.runCommand('dotnet', ['build'], functionsPath);

    // Start func host
    this.functionsProcess = spawn('func', ['start', '--port', '7071'], {
      cwd: functionsPath,
      env: {
        ...process.env,
        AzureWebJobsStorage: this.azuriteConnectionString,
        AZURE_STORAGE_CONNECTION_STRING: this.azuriteConnectionString,
        FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
      }
    });

    if (!this.functionsProcess) {
      throw new Error('Failed to start Azure Functions');
    }

    // Attach output and error handlers
    this.functionsProcess.stdout?.on('data', (data) => {
      console.log(`[Functions] ${data.toString().trim()}`);
    });
    this.functionsProcess.stderr?.on('data', (data) => {
      console.error(`[Functions Error] ${data.toString().trim()}`);
    });
    this.functionsProcess.on('error', (error) => {
      console.error('Functions process error:', error);
    });

    // Wait for Functions to be ready
    await this.waitForService(`${this.functionsUrl}/api/weeks?year=2025`, 90000);
    console.log('Azure Functions started successfully');
  }

  /**
   * Start Blazor Web App
   */
  async startWebApp(): Promise<void> {
    console.log('Starting Web App...');

    const webPath = path.join(this.repoRoot, 'src', 'AgainstTheSpread.Web');

    // Build first
    await this.runCommand('dotnet', ['build'], webPath);

    // Start the app
    this.webProcess = spawn('dotnet', ['run', '--no-build', '--urls', 'http://localhost:5158'], {
      cwd: webPath,
      env: {
        ...process.env,
        API_BASE_URL: this.functionsUrl,
        ASPNETCORE_ENVIRONMENT: 'Development'
      }
    });

    if (!this.webProcess) {
      throw new Error('Failed to start Web App');
    }

    // Attach output and error handlers
    this.webProcess.stdout?.on('data', (data) => {
      console.log(`[Web App] ${data.toString().trim()}`);
    });
    this.webProcess.stderr?.on('data', (data) => {
      console.error(`[Web App Error] ${data.toString().trim()}`);
    });
    this.webProcess.on('error', (error) => {
      console.error('Web App process error:', error);
    });

    // Wait for Web App to be ready
    await this.waitForService(this.webUrl, 90000);
    console.log('Web App started successfully');
  }

  /**
   * Upload weekly lines file to Azurite storage.
   * Creates both Excel (.xlsx) and JSON representations.
   * JSON structure must match the C# WeeklyLines model (AgainstTheSpread.Core.Models.WeeklyLines)
   * with PascalCase properties: Week (number), Year (number), UploadedAt (ISO string), Games (array of game objects).
   * See the C# model for required/optional fields and property details.
   */
  async uploadLinesFile(filePath: string, week: number, year: number): Promise<void> {
    console.log(`Uploading lines for Week ${week}, Year ${year}...`);

    const blobServiceClient = BlobServiceClient.fromConnectionString(this.azuriteConnectionString);
    const containerClient = blobServiceClient.getContainerClient('gamefiles');

    // Create container if it doesn't exist
    await containerClient.createIfNotExists();

    // Upload Excel file
    const excelBlobName = `lines/week-${week}-${year}.xlsx`;
    const excelBlobClient = containerClient.getBlockBlobClient(excelBlobName);
    await excelBlobClient.uploadFile(filePath);

    // Create a JSON representation matching the C# WeeklyLines model
    // Property names must be PascalCase to match C# model
    const jsonBlobName = `lines/week-${week}-${year}.json`;
    const jsonBlobClient = containerClient.getBlockBlobClient(jsonBlobName);

    // Using same date for all test games for simplicity
    const gameDate = new Date();
    const weeklyLinesJson = {
      Week: week,
      Year: year,
      UploadedAt: new Date().toISOString(),
      Games: [
        { Favorite: 'Alabama', Line: -7.0, VsAt: 'vs', Underdog: 'Auburn', GameDate: gameDate.toISOString() },
        { Favorite: 'Georgia', Line: -3.5, VsAt: 'at', Underdog: 'Florida', GameDate: gameDate.toISOString() },
        { Favorite: 'Ohio State', Line: -10.0, VsAt: 'vs', Underdog: 'Michigan', GameDate: gameDate.toISOString() },
        { Favorite: 'Texas', Line: -14.5, VsAt: 'at', Underdog: 'Oklahoma', GameDate: gameDate.toISOString() },
        { Favorite: 'Clemson', Line: -21.0, VsAt: 'vs', Underdog: 'Florida State', GameDate: gameDate.toISOString() },
        { Favorite: 'Notre Dame', Line: -6.5, VsAt: 'at', Underdog: 'USC', GameDate: gameDate.toISOString() },
        { Favorite: 'Penn State', Line: -4.0, VsAt: 'vs', Underdog: 'Michigan State', GameDate: gameDate.toISOString() }
      ]
    };

    const jsonContent = JSON.stringify(weeklyLinesJson, null, 2);
    await jsonBlobClient.upload(jsonContent, jsonContent.length);

    console.log(`Successfully uploaded Week ${week} lines`);
  }

  /**
   * Wait for a service to become available using native http module
   */
  private async waitForService(url: string, timeout: number): Promise<void> {
    const startTime = Date.now();
    let lastError: Error | undefined;
    let attemptCount = 0;

    while (Date.now() - startTime < timeout) {
      attemptCount++;

      const isReady = await new Promise<boolean>((resolve) => {
        const req = http.get(url, (res) => {
          // Service is responding if we get any valid HTTP response
          if (res.statusCode && (res.statusCode < 500 || res.statusCode === 404)) {
            resolve(true);
          } else {
            resolve(false);
          }
          // Drain response to free up socket
          res.resume();
        });

        req.on('error', (error) => {
          lastError = error;
          resolve(false);
        });

        req.setTimeout(5000, () => {
          req.destroy();
          resolve(false);
        });
      });

      if (isReady) {
        console.log(`Service at ${url} is ready after ${attemptCount} attempts`);
        return;
      }

      if (attemptCount % 10 === 0) {
        // Log every 10th attempt
        const errorMsg = lastError ? lastError.message : 'No response';
        console.log(`Attempt ${attemptCount}: Still waiting for ${url} - ${errorMsg}`);
      }

      await new Promise(resolve => setTimeout(resolve, 1000));
    }

    const errorMessage = lastError ? ` Last error: ${lastError.message}` : '';
    throw new Error(`Service at ${url} did not become available within ${timeout / 1000} seconds after ${attemptCount} attempts.${errorMessage}`);
  }

  /**
   * Run a command and wait for it to complete
   */
  private async runCommand(command: string, args: string[], cwd: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const process = spawn(command, args, { cwd });
      let stdout = '';
      let stderr = '';

      if (process.stdout) {
        process.stdout.on('data', (data) => {
          stdout += data.toString();
        });
      }
      if (process.stderr) {
        process.stderr.on('data', (data) => {
          stderr += data.toString();
        });
      }

      process.on('close', (code) => {
        if (code === 0) {
          resolve();
        } else {
          reject(new Error(`Command failed with exit code ${code}\nStdout: ${stdout}\nStderr: ${stderr}`));
        }
      });
      process.on('error', reject);
    });
  }

  /**
   * Cleanup all processes
   */
  async cleanup(): Promise<void> {
    console.log('Shutting down test environment...');

    // Helper to kill and confirm process termination
    const killAndWait = async (proc?: ChildProcess, name?: string): Promise<void> => {
      if (!proc || proc.killed) return;

      proc.kill('SIGTERM');

      // Wait up to 2 seconds for graceful exit
      let exited = false;
      const exitPromise = new Promise<void>(resolve => {
        proc.once('exit', () => {
          exited = true;
          resolve();
        });
      });

      await Promise.race([
        exitPromise,
        new Promise(resolve => setTimeout(resolve, 2000))
      ]);

      if (!exited && proc.pid) {
        console.warn(`Process ${name} did not exit gracefully, sending SIGKILL`);
        proc.kill('SIGKILL');
        await exitPromise;
      }
    };

    await Promise.all([
      killAndWait(this.webProcess, 'webProcess'),
      killAndWait(this.functionsProcess, 'functionsProcess'),
      killAndWait(this.azuriteProcess, 'azuriteProcess')
    ]);

    console.log('Test environment shut down');
  }
}
