import { clsx } from 'clsx';
import styles from './Badge.module.css';

export type BadgeVariant =
  | 'success' | 'warning' | 'critical' | 'info'
  | 'reserved' | 'inactive' | 'expiring'
  | 'primary' | 'gold' | 'default';

interface BadgeProps {
  label: string;
  variant?: BadgeVariant;
  dot?: boolean;
  className?: string;
}

export function Badge({ label, variant = 'default', dot = true, className }: BadgeProps) {
  return (
    <span className={clsx(styles.badge, styles[variant], className)}>
      {dot && <span className={styles.dot} />}
      {label}
    </span>
  );
}
