const data = require('./coverage/coverage-summary.json');
const files = Object.entries(data).filter(([key]) => key !== 'total');

// Categorize files
const highCoverage = [];
const mediumCoverage = [];
const lowCoverage = [];
const zeroCoverage = [];

files.forEach(([path, coverage]) => {
  const pct = coverage.lines.pct;
  const parts = path.split(/[\\\/]/);
  const fileName = parts[parts.length - 1];
  const entry = { fileName, path, pct };

  if (pct >= 80) highCoverage.push(entry);
  else if (pct >= 50) mediumCoverage.push(entry);
  else if (pct > 0) lowCoverage.push(entry);
  else zeroCoverage.push(entry);
});

console.log('\n📊 COVERAGE BREAKDOWN BY FILE');
console.log('='.repeat(80));
console.log('\n✅ HIGH COVERAGE (≥80%): ' + highCoverage.length + ' files');
highCoverage.sort((a, b) => b.pct - a.pct).slice(0, 15).forEach(f => {
  console.log('  ' + f.pct.toFixed(1).padStart(5) + '%  ' + f.fileName);
});

console.log('\n⚠️  MEDIUM COVERAGE (50-79%): ' + mediumCoverage.length + ' files');
mediumCoverage.sort((a, b) => b.pct - a.pct).slice(0, 15).forEach(f => {
  console.log('  ' + f.pct.toFixed(1).padStart(5) + '%  ' + f.fileName);
});

console.log('\n🔴 LOW COVERAGE (1-49%): ' + lowCoverage.length + ' files');
lowCoverage.sort((a, b) => b.pct - a.pct).slice(0, 15).forEach(f => {
  console.log('  ' + f.pct.toFixed(1).padStart(5) + '%  ' + f.fileName);
});

console.log('\n❌ ZERO COVERAGE (0%): ' + zeroCoverage.length + ' files');
console.log('   Sample of files with 0% coverage:');
zeroCoverage.slice(0, 20).forEach(f => {
  console.log('   0.0%  ' + f.fileName);
});

console.log('\n' + '='.repeat(80));
console.log('\n📈 COMPONENT COVERAGE STATISTICS');
console.log('='.repeat(80));

const components = files.filter(([path]) => path.includes('components'));
const pages = files.filter(([path]) => path.includes('app') && path.includes('page.tsx'));
const utils = files.filter(([path]) => path.includes('utils') || path.includes('lib'));
const contexts = files.filter(([path]) => path.includes('contexts'));

const avgCoverage = (arr) => {
  if (arr.length === 0) return 0;
  const sum = arr.reduce((acc, [, cov]) => acc + cov.lines.pct, 0);
  return (sum / arr.length).toFixed(2);
};

console.log('Components:  ' + avgCoverage(components) + '% avg (' + components.length + ' files)');
console.log('Pages:       ' + avgCoverage(pages) + '% avg (' + pages.length + ' files)');
console.log('Utils:       ' + avgCoverage(utils) + '% avg (' + utils.length + ' files)');
console.log('Contexts:    ' + avgCoverage(contexts) + '% avg (' + contexts.length + ' files)');
console.log('='.repeat(80));
