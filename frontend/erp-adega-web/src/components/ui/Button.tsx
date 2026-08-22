import { ButtonHTMLAttributes, forwardRef } from 'react';
import { clsx } from 'clsx';
import styles from './Button.module.css';

export type ButtonVariant = 'primary' | 'secondary' | 'gold' | 'outline' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  icon?: React.ReactNode;
  iconRight?: React.ReactNode;
  fullWidth?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant = 'primary', size = 'md', loading, icon, iconRight, fullWidth, className, children, disabled, ...props }, ref) => {
    return (
      <button
        ref={ref}
        className={clsx(
          styles.btn,
          styles[variant],
          styles[size],
          fullWidth && styles.fullWidth,
          loading && styles.loading,
          className
        )}
        disabled={disabled || loading}
        {...props}
      >
        {loading ? (
          <span className={styles.spinner} />
        ) : icon ? (
          <span className={styles.icon}>{icon}</span>
        ) : null}
        {children && <span>{children}</span>}
        {iconRight && <span className={styles.icon}>{iconRight}</span>}
      </button>
    );
  }
);

Button.displayName = 'Button';
