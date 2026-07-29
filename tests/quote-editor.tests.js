const fs = require("node:fs");
const vm = require("node:vm");
const assert = require("node:assert/strict");

const source = fs.readFileSync(
  "src/Orizon.Distribuidora.Web/wwwroot/js/quote-editor.js",
  "utf8"
);
const context = {
  window: {},
  document: { querySelectorAll: () => [] },
  console
};
vm.createContext(context);
vm.runInContext(source, context);
const editor = context.window.OrizonQuoteEditor;

assert.equal(editor.clampQuantity(1) + 1, 2, "incremento 1 → 2");
assert.equal(editor.clampQuantity(2) - 1, 1, "decremento 2 → 1");
assert.equal(editor.clampQuantity(1) - 1, 0, "o handler deve então aplicar clamp");
assert.equal(editor.clampQuantity(0), 1, "bloqueio abaixo de 1");
assert.equal(editor.parseDecimal("1,5"), 1.5, "parsing pt-BR");
assert.equal(editor.parseDecimal("1.234,56"), 1234.56, "milhar e decimal pt-BR");
assert.equal(editor.roundMoney(2 * 10.015), 20.03, "total atualizado com arredondamento comercial");
assert.equal(editor.roundMoney(1 * 46.30), 46.30, "total inicial de uma unidade");
assert.equal(editor.roundMoney(2 * 46.30), 92.60, "total 1 → 2 altera exatamente uma unidade");

const initializers = (source.match(/quoteEditorInitialized/g) || []).length;
assert.ok(initializers >= 2, "editor possui guarda contra inicialização duplicada");
console.log("quote-editor: 10 verificações aprovadas");
