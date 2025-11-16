import { ChildProcess, spawn } from 'child_process';
import { BlobServiceClient } from '@azure/storage-blob';
import * as path from 'path';
import * as fs from 'fs';

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
        }
      };
      fs.writeFileSync(localSettingsPath, JSON.stringify(localSettings, null, 2));
      console.log('Created local.settings.json for Functions');
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
        API_BASE_URL: this.functionsUrl
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
   * Upload weekly lines file to Azurite storage
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
    
    // Create a simple JSON representation for the API
    const jsonBlobName = `lines/week-${week}-${year}.json`;
    const jsonBlobClient = containerClient.getBlockBlobClient(jsonBlobName);
    
    const weeklyLinesJson = {
      week: week,
      year: year,
      games: [
        { favorite: 'Team A', line: -7.0, vsAt: 'vs', underdog: 'Team B', gameDate: new Date(), gameTime: '12:00 PM' },
        { favorite: 'Team C', line: -3.5, vsAt: 'at', underdog: 'Team D', gameDate: new Date(), gameTime: '3:30 PM' },
        { favorite: 'Team E', line: -10.0, vsAt: 'vs', underdog: 'Team F', gameDate: new Date(), gameTime: '7:00 PM' },
        { favorite: 'Team G', line: -14.5, vsAt: 'at', underdog: 'Team H', gameDate: new Date(), gameTime: '8:00 PM' },
        { favorite: 'Team I', line: -21.0, vsAt: 'vs', underdog: 'Team J', gameDate: new Date(), gameTime: '12:00 PM' },
        { favorite: 'Team K', line: -6.5, vsAt: 'at', underdog: 'Team L', gameDate: new Date(), gameTime: '3:30 PM' },
        { favorite: 'Team M', line: -4.0, vsAt: 'vs', underdog: 'Team N', gameDate: new Date(), gameTime: '7:00 PM' }
      ]
    };
    
    const jsonContent = JSON.stringify(weeklyLinesJson, null, 2);
    await jsonBlobClient.upload(jsonContent, jsonContent.length);
    
    console.log(`Successfully uploaded Week ${week} lines`);
  }

  /**
   * Wait for a service to become available
   */
  private async waitForService(url: string, timeout: number): Promise<void> {
    const startTime = Date.now();
    
    while (Date.now() - startTime < timeout) {
      try {
        const response = await fetch(url);
        if (response.ok || response.status === 404) {
          // Service is responding (even 404 means it's running)
          return;
        }
      } catch {
        // Service not ready yet
      }
      
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
    
    throw new Error(`Service at ${url} did not become available within ${timeout / 1000} seconds`);
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
