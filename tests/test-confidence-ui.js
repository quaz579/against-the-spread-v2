"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g = Object.create((typeof Iterator === "function" ? Iterator : Object).prototype);
    return g.next = verb(0), g["throw"] = verb(1), g["return"] = verb(2), typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (g && (g = 0, op[0] && (_ = 0)), _) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
Object.defineProperty(exports, "__esModule", { value: true });
var playwright_1 = require("playwright");
(function () { return __awaiter(void 0, void 0, void 0, function () {
    var browser, context, page, continueButton, hasError, gameCards, gameCount, game1Card, game1Confidence, game2Card, game2Confidence, option1InGame2, isDisabled, option2InGame1, isOption2Disabled, error_1;
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0: return [4 /*yield*/, playwright_1.chromium.launch({ headless: true })];
            case 1:
                browser = _a.sent();
                return [4 /*yield*/, browser.newContext()];
            case 2:
                context = _a.sent();
                return [4 /*yield*/, context.newPage()];
            case 3:
                page = _a.sent();
                _a.label = 4;
            case 4:
                _a.trys.push([4, 33, 35, 37]);
                console.log('Navigating to bowl picks page...');
                return [4 /*yield*/, page.goto('http://localhost:5158/bowl-picks')];
            case 5:
                _a.sent();
                return [4 /*yield*/, page.waitForLoadState('networkidle')];
            case 6:
                _a.sent();
                // Take screenshot of initial page
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-01-initial.png', fullPage: true })];
            case 7:
                // Take screenshot of initial page
                _a.sent();
                console.log('Screenshot saved: bowl-picks-01-initial.png');
                // Enter name
                return [4 /*yield*/, page.fill('#userName', 'Test User')];
            case 8:
                // Enter name
                _a.sent();
                return [4 /*yield*/, page.selectOption('#year', '2025')];
            case 9:
                _a.sent();
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-02-name-entered.png', fullPage: true })];
            case 10:
                _a.sent();
                console.log('Screenshot saved: bowl-picks-02-name-entered.png');
                continueButton = page.getByRole('button', { name: 'Continue to Bowl Picks' });
                return [4 /*yield*/, continueButton.isVisible()];
            case 11:
                if (!_a.sent()) return [3 /*break*/, 32];
                return [4 /*yield*/, continueButton.click()];
            case 12:
                _a.sent();
                return [4 /*yield*/, page.waitForTimeout(2000)];
            case 13:
                _a.sent();
                return [4 /*yield*/, page.locator('.alert-danger').isVisible().catch(function () { return false; })];
            case 14:
                hasError = _a.sent();
                if (!hasError) return [3 /*break*/, 16];
                console.log('No bowl lines available - this is expected');
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-03-no-lines.png', fullPage: true })];
            case 15:
                _a.sent();
                console.log('Screenshot saved: bowl-picks-03-no-lines.png');
                return [3 /*break*/, 32];
            case 16:
                gameCards = page.locator('.card').filter({ has: page.locator('.card-header') });
                return [4 /*yield*/, gameCards.count()];
            case 17:
                gameCount = _a.sent();
                console.log("Found ".concat(gameCount, " games"));
                if (!(gameCount > 0)) return [3 /*break*/, 32];
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-04-games-loaded.png', fullPage: true })];
            case 18:
                _a.sent();
                console.log('Screenshot saved: bowl-picks-04-games-loaded.png');
                game1Card = gameCards.nth(0);
                game1Confidence = game1Card.locator('select.form-select');
                return [4 /*yield*/, game1Confidence.selectOption('1')];
            case 19:
                _a.sent();
                return [4 /*yield*/, page.waitForTimeout(500)];
            case 20:
                _a.sent();
                // Take screenshot showing first confidence selected
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-05-first-confidence.png', fullPage: true })];
            case 21:
                // Take screenshot showing first confidence selected
                _a.sent();
                console.log('Screenshot saved: bowl-picks-05-first-confidence.png');
                game2Card = gameCards.nth(1);
                game2Confidence = game2Card.locator('select.form-select');
                option1InGame2 = game2Confidence.locator('option[value="1"]');
                return [4 /*yield*/, option1InGame2.getAttribute('disabled')];
            case 22:
                isDisabled = _a.sent();
                console.log("Option 1 in game 2 disabled: ".concat(isDisabled !== null));
                // Scroll down to see second game
                return [4 /*yield*/, game2Card.scrollIntoViewIfNeeded()];
            case 23:
                // Scroll down to see second game
                _a.sent();
                return [4 /*yield*/, page.waitForTimeout(500)];
            case 24:
                _a.sent();
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-06-second-game-option-disabled.png', fullPage: true })];
            case 25:
                _a.sent();
                console.log('Screenshot saved: bowl-picks-06-second-game-option-disabled.png');
                // Select confidence 2 for game 2
                return [4 /*yield*/, game2Confidence.selectOption('2')];
            case 26:
                // Select confidence 2 for game 2
                _a.sent();
                return [4 /*yield*/, page.waitForTimeout(500)];
            case 27:
                _a.sent();
                option2InGame1 = game1Confidence.locator('option[value="2"]');
                return [4 /*yield*/, option2InGame1.getAttribute('disabled')];
            case 28:
                isOption2Disabled = _a.sent();
                console.log("Option 2 in game 1 disabled: ".concat(isOption2Disabled !== null));
                return [4 /*yield*/, game1Card.scrollIntoViewIfNeeded()];
            case 29:
                _a.sent();
                return [4 /*yield*/, page.waitForTimeout(500)];
            case 30:
                _a.sent();
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-07-option-2-disabled-in-game1.png', fullPage: true })];
            case 31:
                _a.sent();
                console.log('Screenshot saved: bowl-picks-07-option-2-disabled-in-game1.png');
                _a.label = 32;
            case 32:
                console.log('Test completed successfully!');
                return [3 /*break*/, 37];
            case 33:
                error_1 = _a.sent();
                console.error('Error during test:', error_1);
                return [4 /*yield*/, page.screenshot({ path: '/tmp/bowl-picks-error.png', fullPage: true })];
            case 34:
                _a.sent();
                return [3 /*break*/, 37];
            case 35: return [4 /*yield*/, browser.close()];
            case 36:
                _a.sent();
                return [7 /*endfinally*/];
            case 37: return [2 /*return*/];
        }
    });
}); })();
