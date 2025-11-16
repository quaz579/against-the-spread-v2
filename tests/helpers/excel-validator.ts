import * as ExcelJS from 'exceljs';

/**
 * Validates the structure and content of a generated Excel picks file
 * @param filePath Path to the Excel file
 * @param expectedName Expected user name
 * @param expectedPickCount Expected number of picks (should be 6)
 */
export async function validateExcelFile(
  filePath: string,
  expectedName: string,
  expectedPickCount: number
): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.readFile(filePath);
  
  const worksheet = workbook.worksheets[0];
  if (!worksheet) {
    throw new Error('No worksheet found in Excel file');
  }

  // Validate structure based on "Weekly Picks Example.xlsx"
  // Row 1: Empty
  const row1Cell = worksheet.getCell('A1').value;
  if (row1Cell !== null && row1Cell !== undefined && row1Cell !== '') {
    throw new Error('Row 1 should be empty');
  }

  // Row 2: Empty
  const row2Cell = worksheet.getCell('A2').value;
  if (row2Cell !== null && row2Cell !== undefined && row2Cell !== '') {
    throw new Error('Row 2 should be empty');
  }

  // Row 3: Headers
  const headers = [
    { cell: 'A3', expected: 'Name' },
    { cell: 'B3', expected: 'Pick 1' },
    { cell: 'C3', expected: 'Pick 2' },
    { cell: 'D3', expected: 'Pick 3' },
    { cell: 'E3', expected: 'Pick 4' },
    { cell: 'F3', expected: 'Pick 5' },
    { cell: 'G3', expected: 'Pick 6' }
  ];

  for (const header of headers) {
    const value = worksheet.getCell(header.cell).value;
    if (value?.toString() !== header.expected) {
      throw new Error(`${header.cell} should be '${header.expected}' but got '${value}'`);
    }
  }

  // Row 4: Data
  const nameCell = worksheet.getCell('A4').value;
  if (nameCell?.toString() !== expectedName) {
    throw new Error(`A4 should contain '${expectedName}' but got '${nameCell}'`);
  }

  // Verify all picks are present
  for (let i = 2; i <= expectedPickCount + 1; i++) {
    const cellAddress = String.fromCharCode(64 + i) + '4';
    const value = worksheet.getCell(cellAddress).value;
    if (!value || value.toString().trim() === '') {
      throw new Error(`Cell ${cellAddress} should contain a pick but is empty`);
    }
  }
}
