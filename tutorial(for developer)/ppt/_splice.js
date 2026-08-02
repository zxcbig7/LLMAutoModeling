const fs=require('fs');
let html=fs.readFileSync('index.html','utf8');
const slides=fs.readFileSync('_slides.html','utf8').trim();
const startTag='<div id="deck">';
const navTag='<div id="nav">';
const si=html.indexOf(startTag);
const ni=html.indexOf(navTag);
if(si<0||ni<0){console.error('markers not found');process.exit(1);}
const before=html.slice(0, si+startTag.length);
const after=html.slice(ni);
const out=before+'\n'+slides+'\n</div>\n\n'+after;
fs.writeFileSync('index.html',out,'utf8');
console.log('spliced. new line count:', out.split('\n').length);
