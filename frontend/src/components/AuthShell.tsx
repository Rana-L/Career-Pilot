import Link from "next/link";
import type { ReactNode } from "react";

const HIGHLIGHTS = [
  "Track every application by stage in one board",
  "AI match scoring against any job description",
  "Tailored CV and cover letter, ready to download",
];

export default function AuthShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-1 flex-col lg:flex-row">
      <aside className="relative hidden flex-col justify-between overflow-hidden bg-indigo-600 p-12 text-white lg:flex lg:w-1/2">
        <div
          aria-hidden
          className="pointer-events-none absolute -right-24 -top-24 h-96 w-96 rounded-full bg-indigo-500/40 blur-3xl"
        />
        <div
          aria-hidden
          className="pointer-events-none absolute -bottom-32 -left-16 h-96 w-96 rounded-full bg-indigo-400/30 blur-3xl"
        />

        <Link href="/" className="relative text-lg font-semibold tracking-tight">
          CareerPilot
        </Link>

        <div className="relative flex flex-col gap-6">
          <h2 className="text-3xl font-semibold leading-tight">
            Your job search, organised and AI-assisted.
          </h2>
          <ul className="flex flex-col gap-3">
            {HIGHLIGHTS.map((item) => (
              <li key={item} className="flex items-start gap-3 text-indigo-50">
                <svg
                  className="mt-0.5 h-5 w-5 shrink-0 text-indigo-200"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  aria-hidden
                >
                  <path
                    fillRule="evenodd"
                    d="M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z"
                    clipRule="evenodd"
                  />
                </svg>
                {item}
              </li>
            ))}
          </ul>
        </div>

        <p className="relative text-sm text-indigo-200">
          Built as a full-stack portfolio project.
        </p>
      </aside>

      <main className="flex flex-1 items-center justify-center bg-zinc-50 px-6 py-12 lg:w-1/2 dark:bg-black">
        <div className="w-full max-w-sm">
          <Link
            href="/"
            className="mb-8 inline-block text-lg font-semibold tracking-tight text-zinc-900 lg:hidden dark:text-white"
          >
            CareerPilot
          </Link>
          {children}
        </div>
      </main>
    </div>
  );
}
