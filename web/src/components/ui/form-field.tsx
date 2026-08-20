import * as React from "react"
import { Label } from "./label"
import { Input } from "./input"

export interface FormFieldProps {
  id: string
  name: string
  label: string
  type?: string
  placeholder?: string
  required?: boolean
  error?: string
  value?: string
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void
  onBlur?: (e: React.FocusEvent<HTMLInputElement>) => void
  disabled?: boolean
  startIcon?: React.ReactNode
  endIcon?: React.ReactNode
  className?: string
  "data-testid"?: string
}

const FormField = React.forwardRef<HTMLInputElement, FormFieldProps>(
  ({ 
    id, 
    name, 
    label, 
    type = "text", 
    placeholder, 
    required, 
    error, 
    value, 
    onChange, 
    onBlur, 
    disabled, 
    startIcon, 
    endIcon, 
    className,
    "data-testid": dataTestId,
    ...props 
  }, ref) => {
    return (
      <div className={`space-y-2 ${className || ""}`}>
        <Label 
          htmlFor={id} 
          required={required} 
          error={!!error}
        >
          {label}
        </Label>
        <Input
          ref={ref}
          id={id}
          name={name}
          type={type}
          placeholder={placeholder}
          value={value}
          onChange={onChange}
          onBlur={onBlur}
          disabled={disabled}
          error={!!error}
          helperText={error}
          startIcon={startIcon}
          endIcon={endIcon}
          data-testid={dataTestId}
          {...props}
        />
      </div>
    )
  }
)

FormField.displayName = "FormField"

export { FormField }