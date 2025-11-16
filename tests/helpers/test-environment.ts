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
      '--silent',
      '--location', azuriteDir,
      '--blobPort', '10000'
    ]);

    // Attach output and error handlers for Azurite process
    this.azuriteProcess.stdout?.on('data', (data) => {
      console.log(`[Azurite] ${data.toString().trim()}`);
    });
    this.azuriteProcess.stderr?.on('data', (data) => {
      console.error(`[Azurite Error] ${data.toString().trim()}`);
    });
    this.azuriteProcess.on('error', (error) => {
      console.error('Azurite process error:', error);
    });
    if (!this.azuriteProcess) {
      throw new Error('Failed to start Azurite');
    }

    // Wait for Azurite to be ready
    await this.waitForService('http://127.0.0.1:10000/devstoreaccount1', 30000);
    console.log('Azurite started successfully');
  }

  /**
   * Start Azure Functions
   */
  async startFunctions(): Promise<void> {
    console.log('Starting Azure Functions...');
    
    const functionsPath = path.join(this.repoRoot, 'src', 'AgainstTheSpread.Functions');

    // Build first
    await this.runCommand('dotnet', ['build'], functionsPath);

    // Start func host
    this.functionsProcess = spawn('func', ['start', '--port', '7071'], {
      cwd: functionsPath,
      env: {
        ...process.env,
        AzureWebJobsStorage: this.azuriteConnectionString,
        AZURE_STORAGE_CONNECTION_STRING: this.azuriteConnectionString
      }
    });

    if (!this.functionsProcess) {
      throw new Error('Failed to start Azure Functions');
    }

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
    
    await jsonBlobClient.upload(JSON.stringify(weeklyLinesJson, null, 2), JSON.stringify(weeklyLinesJson).length);
    
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
      
      process.on('close', (code) => {
        if (code === 0) {
          resolve();
        } else {
          reject(new Error(`Command failed with exit code ${code}`));
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
    
    if (this.webProcess) {
      this.webProcess.kill('SIGTERM');
    }
    
    if (this.functionsProcess) {
      this.functionsProcess.kill('SIGTERM');
    }
    
    if (this.azuriteProcess) {
      this.azuriteProcess.kill('SIGTERM');
    }
    
    // Wait a bit for graceful shutdown
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    console.log('Test environment shut down');
  }
}
