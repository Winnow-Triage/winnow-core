const crypto = require('crypto');

function generateProofOfWork(apiKey, method, path, difficulty = 4) {
  const timestamp = new Date().toISOString();
  let nonce = 0;
  const target = '0'.repeat(difficulty);
  const prefix = `${apiKey}${method.toUpperCase()}${path.toLowerCase()}${timestamp}`;
  while (true) {
    const hash = crypto.createHash('sha256').update(prefix + nonce.toString()).digest('hex');
    if (hash.startsWith(target)) return { timestamp, nonce: nonce.toString() };
    nonce++;
  }
}

async function testApi() {
  const pow = generateProofOfWork('test-key', 'POST', '/reports');
  console.log("PoW:", pow);
  try {
    const response = await fetch('http://localhost:5294/reports', {
      method: 'POST',
      headers: {
        'X-Winnow-Key': 'test-key',
        'X-Winnow-PoW-Nonce': pow.nonce,
        'X-Winnow-PoW-Timestamp': pow.timestamp,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        Title: "Test",
        Message: "Message"
      })
    });
    console.log("Status:", response.status);
    const text = await response.text();
    console.log("Response:", text);
  } catch (e) {
    console.error(e);
  }
}
testApi();
