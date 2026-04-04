import { createHash } from 'node:crypto';

/**
 * Generates a SHA256 Proof-of-Work nonce and timestamp for Winnow API requests.
 * Matches the validation logic in Winnow.API.Infrastructure.Security.PoW.PoWValidator.
 */
export function generateProofOfWork(
  apiKey: string,
  method: string,
  path: string,
  difficulty: number = 4
): { timestamp: string; nonce: string } {
  const timestamp = new Date().toISOString();
  let nonce = 0;
  const target = '0'.repeat(difficulty);
  
  // Data format: ApiKey + Method.ToUpper + Path.ToLower + Timestamp + Nonce
  const prefix = `${apiKey}${method.toUpperCase()}${path.toLowerCase()}${timestamp}`;

  while (true) {
    const hash = createHash('sha256')
      .update(prefix + nonce.toString())
      .digest('hex');

    if (hash.startsWith(target)) {
      return { timestamp, nonce: nonce.toString() };
    }
    nonce++;
    
    // Safety break for tests
    if (nonce > 1_000_000) {
      throw new Error(`Failed to generate PoW after 1M iterations for ${path}`);
    }
  }
}
