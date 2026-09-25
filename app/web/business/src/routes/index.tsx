import { createFileRoute, Link } from "@tanstack/react-router";
import { ManageCookiesButton } from "@concertable/web/components/ManageCookiesButton";

function MarketingPage() {
  return (
    <div className="flex min-h-dvh flex-col bg-accent">
      <header className="bg-primary px-8 py-14 text-center text-primary-foreground">
        <img
          src="/logo-long.png"
          alt="Concertable"
          className="mx-auto mb-5 h-10 brightness-0 invert"
        />
        <p className="text-sm text-primary-foreground/80">
          Booking, production and settlement for every concert business.
        </p>
      </header>
      <main className="mx-auto flex w-full max-w-3xl flex-1 flex-col items-center justify-center gap-6 px-8 py-12 text-center">
        <h1 className="text-4xl font-semibold">Run the business behind the show</h1>
        <p className="max-w-2xl text-muted-foreground">
          Work as a venue, artist, promoter or neutral production business from one organization account.
        </p>
        <div className="flex gap-3">
          <Link className="rounded-md bg-primary px-5 py-3 font-medium text-primary-foreground" to="/login" search={{ redirect: "/app" }}>
            Sign in
          </Link>
          <Link className="rounded-md border border-border px-5 py-3 font-medium" to="/register">
            Create account
          </Link>
        </div>
      </main>
      <footer className="flex justify-center gap-4 px-6 pb-10 text-xs text-muted-foreground">
        <a href="/privacy">Privacy</a>
        <ManageCookiesButton className="text-muted-foreground" />
      </footer>
    </div>
  );
}

export const Route = createFileRoute("/")({ component: MarketingPage });
