export const STATUS_BADGE_STYLES: string[] = [
  "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300", // Wishlist
  "bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300", // Applied
  "bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300", // Assessment
  "bg-purple-100 text-purple-700 dark:bg-purple-950 dark:text-purple-300", // Interview
  "bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-300", // Offer
  "bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300", // Rejected
];

export function scoreColor(score: number): string {
  if (score >= 70) return "text-green-600 dark:text-green-400";
  if (score >= 40) return "text-amber-600 dark:text-amber-400";
  return "text-red-600 dark:text-red-400";
}

export function scoreBarColor(score: number): string {
  if (score >= 70) return "bg-green-500";
  if (score >= 40) return "bg-amber-500";
  return "bg-red-500";
}
