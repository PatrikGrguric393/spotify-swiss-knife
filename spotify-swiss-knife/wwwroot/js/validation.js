class FormValidator {
	constructor(formSelector) {
		try {
			this.form = document.querySelector(formSelector);
		} catch (e) {
			this.form = null;
		}
		if (!this.form) {
			console.error(`Form not found: ${formSelector}`);
			return;
		}

		if (this.form.dataset.validationInitialized === 'true') {
			return;
		}

		this.fields = {};
		this.checkboxGroups = {};
		this.hasErrors = false;
		this.init();
	}

	init() {
		this.form.dataset.validationInitialized = 'true';
		this.form.setAttribute('novalidate', 'novalidate');
		this.formError = this.form.querySelector('[data-form-error]');
		const inputs = this.form.querySelectorAll('input, textarea, select');
		inputs.forEach((input) => {
			if (input.type === 'hidden' || input.type === 'checkbox') return;
			const fieldName = input.name;
			if (fieldName) {
				this.fields[fieldName] = {
					element: input,
					isValid: true,
					errorContainer: null,
				};

				input.addEventListener('blur', () => this.validateField(fieldName));
				if (input.hasAttribute('data-match')) {
					input.addEventListener('input', () => this.validateField(fieldName));
				} else {
					input.addEventListener('input', () => this.clearFieldError(fieldName));
				}
				this.createErrorContainer(input, fieldName);

				if (this.fields[fieldName].errorContainer?.classList.contains('show')) {
					input.classList.add('field-error');
					this.fields[fieldName].isValid = false;
				}
			}
		});

		Object.keys(this.fields).forEach((fieldName) => {
			const input = this.fields[fieldName].element;
			if (!input.hasAttribute('data-match')) return;
			const source = this.resolveMatchSource(input);
			if (source) {
				source.addEventListener('input', () => this.validateField(fieldName));
			}
		});

		this.form.querySelectorAll('fieldset[data-required-group]').forEach((fieldset) => {
			const firstCheckbox = fieldset.querySelector('input[type="checkbox"]');
			if (!firstCheckbox || !firstCheckbox.name) return;
			const groupName = firstCheckbox.name;
			const errorContainer = fieldset.querySelector(`#error-${groupName}`) || this.form.querySelector(`#error-${groupName}`);
			this.checkboxGroups[groupName] = { fieldset, errorContainer, isValid: true };
			if (errorContainer?.classList.contains('show')) {
				this.checkboxGroups[groupName].isValid = false;
			}
			fieldset.querySelectorAll('input[type="checkbox"]').forEach((cb) => {
				cb.addEventListener('change', () => this.validateCheckboxGroup(groupName));
			});
		});

		this.form.addEventListener('submit', (e) => this.handleSubmit(e));
	}

	validateCheckboxGroup(groupName) {
		const group = this.checkboxGroups[groupName];
		if (!group) return true;
		const checkboxes = this.form.querySelectorAll(`input[type="checkbox"][name="${groupName}"]`);
		const anyChecked = Array.from(checkboxes).some((cb) => cb.checked);
		const container = group.errorContainer;
		const message = group.fieldset.dataset.requiredGroupMessage || 'Select at least one option.';
		if (!anyChecked) {
			if (container) {
				container.classList.add('show');
				container.innerHTML = `<div class="validation-error-message">${message}</div>`;
			}
			group.isValid = false;
		} else {
			if (container) {
				container.classList.remove('show');
				container.innerHTML = '';
			}
			group.isValid = true;
		}
		return anyChecked;
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

		const formatErrorStart = errors.length;

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
				const emailPattern = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$/;
				if (!emailPattern.test(value)) {
					const label = this.getFieldLabel(input);
					errors.push(`${label} must be a valid email address`);
				}
			}
		}

		const title = input.getAttribute('title');
		if (title && errors.length > formatErrorStart) {
			errors.splice(formatErrorStart, errors.length - formatErrorStart, title);
		}

		if (input.hasAttribute('data-match') && value) {
			const source = this.resolveMatchSource(input);
			if (source && source.value !== input.value) {
				const label = this.getFieldLabel(input);
				errors.push(`${label} does not match`);
			}
		}

		const isValid = errors.length === 0;
		this.displayFieldErrors(fieldName, errors, isValid);

		return isValid;
	}

	resolveMatchSource(input) {
		const ref = input.getAttribute('data-match');
		if (!ref) return null;
		let source = null;
		if (ref.charAt(0) === '#') {
			source = document.getElementById(ref.slice(1));
		}
		if (!source) {
			source = this.form.querySelector(`#${ref}`) || this.form.querySelector(`[name="${ref}"]`);
		}
		return source;
	}

	displayFieldErrors(fieldName, errors, isValid) {
		const field = this.fields[fieldName];
		const input = field.element;
		const container = field.errorContainer;

		if (isValid && input.dataset.asyncError) {
			field.isValid = false;
			return;
		}

		field.isValid = isValid;

		if (isValid) {
			input.classList.remove('field-error');
			if (input.value.trim()) {
				input.classList.add('field-success');
			} else {
				input.classList.remove('field-success');
			}
			input.setAttribute('aria-invalid', 'false');
			container.classList.remove('show');
			container.innerHTML = '';
		} else {
			input.classList.remove('field-success');
			input.classList.add('field-error');
			input.setAttribute('aria-invalid', 'true');
			container.classList.add('show');
			container.innerHTML = errors
				.map((error) => `<div class="validation-error-message">${this.escapeHtml(error)}</div>`)
				.join('');
		}
	}

	clearFieldError(fieldName) {
		const field = this.fields[fieldName];
		if (field && !field.isValid) {
			delete field.element.dataset.asyncError;
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
			this.validateField(fieldName);
			if (!this.fields[fieldName].isValid) allValid = false;
		});
		Object.keys(this.checkboxGroups).forEach((groupName) => {
			if (!this.validateCheckboxGroup(groupName)) allValid = false;
		});
		return allValid;
	}

	handleSubmit(e) {
		if (!this.validateAll()) {
			e.preventDefault();
			this.showFormError('Please fix the highlighted fields before continuing.');
			for (const fieldName in this.fields) {
				if (!this.fields[fieldName].isValid) {
					this.fields[fieldName].element.focus();
					return;
				}
			}
			for (const groupName in this.checkboxGroups) {
				if (!this.checkboxGroups[groupName].isValid) {
					const firstCb = this.form.querySelector(`input[type="checkbox"][name="${groupName}"]`);
					if (firstCb) firstCb.focus();
					return;
				}
			}
		} else {
			this.hideFormError();
		}
	}

	showFormError(message) {
		if (!this.formError) return;
		this.formError.textContent = message;
		this.formError.classList.add('show');
	}

	hideFormError() {
		if (!this.formError) return;
		this.formError.textContent = '';
		this.formError.classList.remove('show');
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
		if (!form.id) return;
		new FormValidator(`#${form.id}`);
	});
});

window.FormValidator = FormValidator;
