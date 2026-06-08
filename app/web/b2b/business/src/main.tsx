import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "shared/index.css";

function BuildingIcon() {
  return (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18Z" />
      <path d="M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2" />
      <path d="M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2" />
      <path d="M10 6h4M10 10h4M10 14h4M10 18h4" />
    </svg>
  );
}

function MusicIcon() {
  return (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 18V5l12-2v13" />
      <circle cx="6" cy="18" r="3" />
      <circle cx="18" cy="16" r="3" />
    </svg>
  );
}

interface CardProps {
  testId: string;
  href: string;
  icon: React.ReactNode;
  title: string;
  description: string;
}

function ArrowRightIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5 12h14M12 5l7 7-7 7" />
    </svg>
  );
}

function GatewayCard({ testId, href, icon, title, description }: CardProps) {
  return (
    <a
      data-testid={testId}
      href={href}
      className="group flex flex-col gap-5 rounded-2xl border border-border bg-white p-8 text-inherit no-underline transition-all hover:-translate-y-1 hover:border-primary hover:shadow-lg"
    >
      <div className="flex h-14 w-14 items-center justify-center rounded-xl bg-primary/10 text-primary">
        {icon}
      </div>
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold text-foreground">{title}</h2>
        <p className="text-sm text-muted-foreground leading-relaxed">{description}</p>
      </div>
      <div className="mt-auto flex items-center gap-1.5 text-sm font-medium text-primary">
        Get started
        <ArrowRightIcon />
      </div>
    </a>
  );
}

function App() {
  const venueUrl = import.meta.env.VITE_VENUE_WEB_URL as string;
  const artistUrl = import.meta.env.VITE_ARTIST_WEB_URL as string;
  const customerUrl = import.meta.env.VITE_CUSTOMER_WEB_URL as string;

  return (
    <div className="flex min-h-dvh flex-col bg-accent">
      <header className="bg-primary px-8 pb-12 pt-14 text-center text-primary-foreground">
        <div className="absolute right-6 top-5">
          <a
            href={`${venueUrl}/login`}
            data-testid="header-login"
            className="text-sm font-medium text-primary-foreground/80 hover:text-primary-foreground"
          >
            Sign in
          </a>
        </div>
        <img src="/logo-long.png" alt="Concertable" className="mx-auto mb-5 h-10 brightness-0 invert" />
        <p className="text-sm leading-relaxed text-primary-foreground/80">
          Whether you book shows or play them, this is where the work happens.
        </p>
      </header>

      <main className="flex flex-1 items-center justify-center px-8 py-12">
        <div className="grid w-full max-w-3xl grid-cols-2 gap-6">
        <GatewayCard
          testId="get-started-venue"
          href={`${venueUrl}/login`}
          icon={<BuildingIcon />}
          title="I manage a venue"
          description="Post opportunities, review applications, run shows and settle with artists."
        />
        <GatewayCard
          testId="get-started-artist"
          href={`${artistUrl}/login`}
          icon={<MusicIcon />}
          title="I represent artists"
          description="List your artists, apply to opportunities, manage bookings and get paid."
        />
        </div>
      </main>

      <footer className="mt-auto flex flex-col gap-1.5 px-6 pb-10 text-center">
        <p className="text-sm text-muted-foreground">
          Already have an account?{" "}
          <a href={`${venueUrl}/login`} className="font-medium text-primary hover:underline">
            Sign in
          </a>
        </p>
        <p className="text-xs text-muted-foreground/60">
          Looking to buy tickets?{" "}
          <a href={customerUrl} className="font-semibold text-primary hover:underline">
            Go to concertable.com
          </a>
        </p>
      </footer>
    </div>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
