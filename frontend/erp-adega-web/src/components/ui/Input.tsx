import { InputHTMLAttributes, forwardRef } from 'react';
import { clsx } from 'clsx';
import styles from './Input.module.css';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helper?: string;
  icon?: React.ReactNode;
  fullWidth?: boolean;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helper, icon, fullWidth = true, className, id, ...props }, ref) => {
    const inputId = id || label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className={clsx(styles.wrapper, fullWidth && styles.fullWidth, className)}>
        {label && (
          <label htmlFor={inputId} className={styles.label}>
            {label}
          </label>
        )}
        <div className={clsx(styles.inputWrapper, error && styles.hasError, props.disabled && styles.disabled)}>
          {icon && <span className={styles.icon}>{icon}</span>}
          <input
            ref={ref}
            id={inputId}
            className={styles.input}
            {...props}
          />
        </div>
        {(error || helper) && (
          <span className={clsx(styles.hint, error && styles.hintError)}>
            {error || helper}
          </span>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';
