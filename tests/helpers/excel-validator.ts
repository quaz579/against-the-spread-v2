import * as ExcelJS from 'exceljs';

/**
 * Validation result from Excel file checks
 */
export interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Validates an Excel file generated from user picks
 * Expected structure:
 * - Row 1: Empty
 * - Row 2: Empty  
 * - Row 3: Headers (Name, Pick 1, Pick 2, ..., Pick 6)
 * - Row 4: User data (name and 6 picks)
 * 
 * @param filePath - Path to the Excel file to validate
 * @param expectedName - Expected name in the picks file
 * @param expectedPickCount - Number of picks expected (default: 6)
 * @returns Validation result with errors if any
 */
export async function validatePicksExcel(
  filePath: string,
  expectedName: string,
  expectedPickCount: number = 6
): Promise<ValidationResult> {
  const errors: string[] = [];
  
  try {
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.readFile(filePath);
    
    const worksheet = workbook.worksheets[0];
    if (!worksheet) {
      return { isValid: false, errors: ['No worksheet found in Excel file'] };
    }
    
    // Check Row 1 is empty (ExcelJS uses 1-based indexing, so values[0] is always undefined)
    const row1 = worksheet.getRow(1);
    const row1Values = row1.values;
    if (row1Values && Array.isArray(row1Values) && row1Values.slice(1).some(v => v !== undefined && v !== null && v !== '')) {
      errors.push(`Row 1 should be empty but contains data`);
    }
    
    // Check Row 2 is empty
    const row2 = worksheet.getRow(2);
    const row2Values = row2.values;
    if (row2Values && Array.isArray(row2Values) && row2Values.slice(1).some(v => v !== undefined && v !== null && v !== '')) {
      errors.push(`Row 2 should be empty but contains data`);
    }
    
    // Check Row 3 headers
    const row3 = worksheet.getRow(3);
    const expectedHeaders = ['Name', ...Array.from({ length: expectedPickCount }, (_, i) => `Pick ${i + 1}`)];
    
    for (let i = 0; i < expectedHeaders.length; i++) {
      const cellValue = row3.getCell(i + 1).value?.toString();
      if (cellValue !== expectedHeaders[i]) {
        errors.push(`Header at column ${i + 1} should be "${expectedHeaders[i]}" but was "${cellValue}"`);
      }
    }
    
    // Check Row 4 data
    const row4 = worksheet.getRow(4);
    const actualName = row4.getCell(1).value?.toString();
    
    if (actualName !== expectedName) {
      errors.push(`Name should be "${expectedName}" but was "${actualName}"`);
    }
    
    // Check all picks are present
    let actualPickCount = 0;
    for (let i = 2; i <= expectedPickCount + 1; i++) {
      const pickValue = row4.getCell(i).value;
      if (pickValue !== null && pickValue !== undefined && pickValue !== '') {
        actualPickCount++;
      }
    }
    
    if (actualPickCount !== expectedPickCount) {
      errors.push(`Expected ${expectedPickCount} picks but found ${actualPickCount}`);
    }
    
    return {
      isValid: errors.length === 0,
      errors
    };
    
  } catch (error) {
    return {
      isValid: false,
      errors: [`Failed to read Excel file: ${error}`]
    };
  }
}

/**
 * Get all picks from the Excel file
 * 
 * @param filePath - Path to the Excel file
 * @returns Array of pick values
 */
export async function getPicksFromExcel(filePath: string): Promise<string[]> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(filePath);
  
  const worksheet = workbook.worksheets[0];
  if (!worksheet) {
    return [];
  }
  
  const row4 = worksheet.getRow(4);
  const picks: string[] = [];
  
  // Picks are in columns 2-7 (Pick 1 through Pick 6)
  for (let i = 2; i <= 7; i++) {
    const pickValue = row4.getCell(i).value?.toString();
    if (pickValue) {
      picks.push(pickValue);
    }
  }
  
  return picks;
}
