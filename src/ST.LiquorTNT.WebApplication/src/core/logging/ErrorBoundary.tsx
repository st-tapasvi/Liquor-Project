import { Component, type ErrorInfo, type ReactNode } from 'react';

import { logger } from './logger';

interface ErrorBoundaryProps {
  fallback: (args: { error: Error; reset: () => void }) => ReactNode;
  scope: string;
  children: ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
}

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
