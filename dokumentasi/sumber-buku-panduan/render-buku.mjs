// Render buku.html -> PDF A4 (pipeline sama dengan referensi Kopi Kuliahan).
// Jalankan dari folder yang berisi node_modules (puppeteer-core) + buku.html.
import puppeteer from 'puppeteer-core';
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const SRC = new URL('./buku.html', import.meta.url).pathname;
const DEST = '/Users/izhardwiputra/Claude-Projects/VR02-UAS-Doll-House/dokumentasi/Panduan-Bedah-Kode-Wahana-Rumah-Boneka.pdf';
const sleep = (ms) => new Promise(r => setTimeout(r, ms));

const browser = await puppeteer.launch({ executablePath: CHROME, headless: 'new', args: ['--no-sandbox'] });
const page = await browser.newPage();
await page.goto('file://' + SRC, { waitUntil: 'networkidle0', timeout: 60000 });
await page.evaluate(async () => { await document.fonts.ready; });
await sleep(600);

await page.pdf({
  path: DEST,
  format: 'A4',
  printBackground: true,
  displayHeaderFooter: true,
  headerTemplate: '<span></span>',
  footerTemplate: '<div style="width:100%; font-size:7.5px; color:#6B6880; font-family:sans-serif; text-align:center; padding:0 10mm;">Panduan Bedah Kode - Wahana Rumah Boneka (Kelompok 1) &middot; hal. <span class="pageNumber"></span> / <span class="totalPages"></span></div>',
  margin: { top: '15mm', bottom: '16mm', left: '18mm', right: '18mm' },
});

await browser.close();
console.log('DONE -> ' + DEST);
