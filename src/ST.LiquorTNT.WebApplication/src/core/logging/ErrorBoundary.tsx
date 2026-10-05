import { Component, type ErrorInfo, type ReactNode } from 'react';

import { logger } from './logger';

interface ErrorBoundaryProps {
  /** Rendered instead of the children after a render error. Receives the error and a reset callback. */
  fallback: (args: { error: Error; reset: () => void }) => ReactNode;
  /** Name that appears in the log so the failing area is obvious. */
  scope: string;
  children: ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
}

/**
 * The one class component in the codebase: React error boundaries have no hook equivalent.
 * Route-level boundaries use React Router's `errorElement` (see core/router); this one wraps the
 * whole tree so a crash in a provider still shows a page instead of a blank screen.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  override state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  override componentDidCatch(error: Error, info: ErrorInfo): void {
    logger.error('render error', { scope: this.props.scope, error, componentStack: info.componentStack });
    logger.flush();
  }

  private readonly reset = (): void => {
    this.setState({ error: null });
  };

  override render(): ReactNode {
    const { error } = this.state;
    if (error) return this.props.fallback({ error, reset: this.reset });
    return this.props.children;
  }
}
