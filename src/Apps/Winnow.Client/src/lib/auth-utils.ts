export const passwordRules = [
  {
    label: "Between 8 and 128 characters",
    test: (p: string) => p.length >= 8 && p.length <= 128,
  },
  {
    label: "At least one uppercase letter",
    test: (p: string) => /[A-Z]/.test(p),
  },
  {
    label: "At least one lowercase letter",
    test: (p: string) => /[a-z]/.test(p),
  },
  { label: "At least one number", test: (p: string) => /[0-9]/.test(p) },
  {
    label: "At least one special character",
    test: (p: string) => /[^A-Za-z0-9]/.test(p),
  },
];

export function validatePassword(password: string): boolean {
  return passwordRules.every((rule) => rule.test(password));
}
