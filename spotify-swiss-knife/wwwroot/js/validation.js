class FormValidator {
	constructor(formSelector) {
		this.form = document.querySelector(formSelector);
		if (!this.form) {
			console.error(`Form not found: ${formSelector}`);
			return;
		}

		if (this.form.dataset.validationInitialized === 'true') {
			return;
		}

		this.fields = {};
		this.hasErrors = false;
		this.init();
	}

	init() {
		this.form.dataset.validationInitialized = 'true';
		const inputs = this.form.querySelectorAll('input, textarea, select');
		inputs.forEach((input) => {
			const fieldName = input.name;
			if (fieldName) {
				this.fields[fieldName] = {
					element: input,
					isValid: true,
					errorContainer: null,
				};

				input.addEventListener('blur', () => this.validateField(fieldName));
				input.addEventListener('input', () => this.clearFieldError(fieldName));
				this.createErrorContainer(input, fieldName);
			}
		});

		this.form.addEventListener('submit', (e) => this.handleSubmit(e));
	}

	createErrorContainer(input, fieldName) {
		const existing = this.form.querySelector(`#error-${fieldName}`);
		if (existing) {
			this.fields[fieldName].errorContainer = existing;
			return;
		}
		const container = document.createElement('div');
		container.className = 'validation-error';
		container.setAttribute('role', 'alert');
		container.setAttribute('aria-live', 'polite');
		container.id = `error-${fieldName}`;
		input.parentNode.insertBefore(container, input.nextSibling);
		this.fields[fieldName].errorContainer = container;
	}

	validateField(fieldName) {
		const field = this.fields[fieldName];
		if (!field) return true;

		const input = field.element;
		const value = input.value.trim();

		let errors = [];

		if (input.hasAttribute('required') && !value) {
			const label = this.getFieldLabel(input);
			errors.push(`${label} is required`);
		}

		if (value) {
			if (input.hasAttribute('data-val-length-max')) {
				const maxLength = parseInt(input.getAttribute('data-val-length-max'));
				const minLength = input.getAttribute('data-val-length-min')
					? parseInt(input.getAttribute('data-val-length-min'))
					: 0;

				if (value.length > maxLength) {
					const label = this.getFieldLabel(input);
					errors.push(`${label} must not exceed ${maxLength} characters`);
				}
				if (value.length < minLength && minLength > 0) {
					const label = this.getFieldLabel(input);
					errors.push(`${label} must be at least ${minLength} characters`);
				}
			}

			if (input.type === 'number') {
				const numValue = parseFloat(value);
				if (input.hasAttribute('min')) {
					const min = parseFloat(input.getAttribute('min'));
					if (numValue < min) {
						const label = this.getFieldLabel(input);
						errors.push(`${label} must be at least ${min}`);
					}
				}
				if (input.hasAttribute('max')) {
					const max = parseFloat(input.getAttribute('max'));
					if (numValue > max) {
						const label = this.getFieldLabel(input);
						errors.push(`${label} must not exceed ${max}`);
					}
				}
			}

			if (input.hasAttribute('pattern')) {
				const pattern = new RegExp(`^${input.getAttribute('pattern')}$`);
				if (!pattern.test(value)) {
					const label = this.getFieldLabel(input);
					const title = input.getAttribute('title') || 'invalid format';
					errors.push(`${label}: ${title}`);
				}
			}

			if (input.type === 'email') {
				const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
				if (!emailPattern.test(value)) {
					const label = this.getFieldLabel(input);
					errors.push(`${label} must be a valid email address`);
				}
			}
		}

		const isValid = errors.length === 0;
		this.displayFieldErrors(fieldName, errors, isValid);

		return isValid;
	}

	displayFieldErrors(fieldName, errors, isValid) {
		const field = this.fields[fieldName];
		const input = field.element;
		const container = field.errorContainer;

		field.isValid = isValid;

		if (isValid) {
			input.classList.remove('field-error');
			input.classList.add('field-success');
			container.classList.remove('show');
			container.innerHTML = '';
		} else {
			input.classList.remove('field-success');
			input.classList.add('field-error');
			container.classList.add('show');
			container.innerHTML = errors
				.map((error) => `<div class="validation-error-message">${this.escapeHtml(error)}</div>`)
				.join('');
		}
	}

	clearFieldError(fieldName) {
		const field = this.fields[fieldName];
		if (field && !field.isValid) {
			field.element.classList.remove('field-error', 'field-success');
			field.errorContainer.classList.remove('show');
			field.errorContainer.innerHTML = '';
		}
	}

	getFieldLabel(input) {
		const labelElement = document.querySelector(`label[for="${input.id}"]`);
		if (labelElement) {
			return labelElement.textContent.replace(/\s*\*\s*$/, '').trim();
		}
		return input.name || 'Field';
	}

	validateAll() {
		let allValid = true;
		Object.keys(this.fields).forEach((fieldName) => {
			const isValid = this.validateField(fieldName);
			if (!isValid) allValid = false;
		});
		return allValid;
	}

	handleSubmit(e) {
		if (!this.validateAll()) {
			e.preventDefault();
			for (const fieldName in this.fields) {
				if (!this.fields[fieldName].isValid) {
					this.fields[fieldName].element.focus();
					break;
				}
			}
		}
	}

	escapeHtml(text) {
		const div = document.createElement('div');
		div.textContent = text;
		return div.innerHTML;
	}
}

document.addEventListener('DOMContentLoaded', () => {
	const forms = document.querySelectorAll('form[data-validate="true"]');
	forms.forEach((form) => {
		new FormValidator(`#${form.id || ''}`);
	});
});

window.FormValidator = FormValidator;
