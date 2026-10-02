import { AuthCard } from '../components/AuthCard';
import { LoginForm } from '../components/LoginForm';
import { SessionEndedNotice } from '../components/SessionEndedNotice';

export default function LoginPage() {
  return (
    <AuthCard>
      <SessionEndedNotice />
      <LoginForm />
    </AuthCard>
  );
}
