export default function Home() {
  return (
    <div className="flex flex-1 flex-col items-center justify-center bg-zinc-50 dark:bg-black">
      <main className="flex w-full max-w-xl flex-col items-center gap-6 px-6 text-center">
        <h1 className="text-4xl font-semibold tracking-tight text-black dark:text-zinc-50">
          CareerPilot
        </h1>
        <p className="text-lg leading-8 text-zinc-600 dark:text-zinc-400">
          Track your job applications, get AI-driven CV feedback, and see your
          job search progress at a glance.
        </p>
      </main>
    </div>
  );
}
