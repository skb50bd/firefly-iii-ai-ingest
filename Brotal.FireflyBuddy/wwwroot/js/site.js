// Custom JavaScript for Firefly Buddy

// Theme Management
function initializeTheme() {
  const savedTheme = localStorage.getItem('theme') || 'dark';
  document.body.className = savedTheme === 'light' ? 'light' : '';
  updateThemeIcon(savedTheme);
}

function toggleTheme() {
  const currentTheme = document.body.classList.contains('light') ? 'light' : 'dark';
  const newTheme = currentTheme === 'light' ? 'dark' : 'light';

  document.body.className = newTheme === 'light' ? 'light' : '';
  localStorage.setItem('theme', newTheme);
  updateThemeIcon(newTheme);
}

function updateThemeIcon(theme) {
  const themeIcon = document.getElementById('theme-icon');
  if (themeIcon) {
    themeIcon.className = theme === 'light' ? 'bi bi-moon' : 'bi bi-sun';
  }
}

document.addEventListener('DOMContentLoaded', function() {
  // Initialize theme
  initializeTheme();

  // Auto-dismiss alerts after 5 seconds
  const alerts = document.querySelectorAll('.alert');
  alerts.forEach(alert => {
    setTimeout(() => {
      alert.style.opacity = '0';
      setTimeout(() => {
        alert.remove();
      }, 300);
    }, 5000);
  });

  // Add loading states to forms
  const forms = document.querySelectorAll('form');
  forms.forEach(form => {
    form.addEventListener('submit', function() {
      const submitBtn = form.querySelector('button[type="submit"]');
      if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner"></span>Processing...';
      }
    });
  });

  // Add confirmation for destructive actions
  const deleteButtons = document.querySelectorAll('[data-confirm]');
  deleteButtons.forEach(button => {
    button.addEventListener('click', function(e) {
      const message = this.getAttribute('data-confirm');
      if (!confirm(message)) {
        e.preventDefault();
      }
    });
  });

  // Initialize tooltips
  const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
  tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });

  // Initialize popovers
  const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
  popoverTriggerList.map(function (popoverTriggerEl) {
    return new bootstrap.Popover(popoverTriggerEl);
  });

  // Add copy to clipboard functionality
  const copyButtons = document.querySelectorAll('[data-copy]');
  copyButtons.forEach(button => {
    button.addEventListener('click', function() {
      const text = this.getAttribute('data-copy');
      navigator.clipboard.writeText(text).then(() => {
        // Show temporary success message
        const originalText = this.innerHTML;
        this.innerHTML = '<i class="bi bi-check"></i> Copied!';
        this.classList.add('btn-success');
        this.classList.remove('btn-outline-secondary');

        setTimeout(() => {
          this.innerHTML = originalText;
          this.classList.remove('btn-success');
          this.classList.add('btn-outline-secondary');
        }, 2000);
      });
    });
  });

  // Auto-save draft changes (if needed)
  const draftForm = document.querySelector('form[data-auto-save]');
  if (draftForm) {
    let saveTimeout;
    const inputs = draftForm.querySelectorAll('input, textarea, select');

    inputs.forEach(input => {
      input.addEventListener('input', function() {
        clearTimeout(saveTimeout);
        saveTimeout = setTimeout(() => {
          // Auto-save logic here if needed
          console.log('Auto-saving draft...');
        }, 2000);
      });
    });
  }

  // Add keyboard shortcuts
  document.addEventListener('keydown', function(e) {
    // Ctrl/Cmd + S to save forms
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault();
      const form = document.querySelector('form');
      if (form) {
        form.requestSubmit();
      }
    }

    // Escape to close modals/alerts
    if (e.key === 'Escape') {
      const alerts = document.querySelectorAll('.alert');
      alerts.forEach(alert => {
        alert.style.opacity = '0';
        setTimeout(() => {
          alert.remove();
        }, 300);
      });
    }
  });

  // Add smooth scrolling for anchor links
  const anchorLinks = document.querySelectorAll('a[href^="#"]');
  anchorLinks.forEach(link => {
    link.addEventListener('click', function(e) {
      e.preventDefault();
      const target = document.querySelector(this.getAttribute('href'));
      if (target) {
        target.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });
      }
    });
  });

  // Add loading states for AJAX requests
  const ajaxLinks = document.querySelectorAll('[data-ajax]');
  ajaxLinks.forEach(link => {
    link.addEventListener('click', function(e) {
      e.preventDefault();
      const url = this.getAttribute('href');
      const target = this.getAttribute('data-target');

      if (target) {
        const targetElement = document.querySelector(target);
        if (targetElement) {
          targetElement.innerHTML = '<div class="text-center"><div class="spinner" role="status"><span class="visually-hidden">Loading...</span></div></div>';

          fetch(url)
            .then(response => response.text())
            .then(html => {
              targetElement.innerHTML = html;
            })
            .catch(error => {
              targetElement.innerHTML = '<div class="alert alert-danger">Error loading content</div>';
            });
        }
      }
    });
  });

  // Initialize any custom components
  initializeCustomComponents();
});

function initializeCustomComponents() {
  // Add any custom component initialization here
  console.log('Firefly Buddy initialized');
}

// Utility functions
function showToast(message, type = 'info') {
  const toastContainer = document.querySelector('.toast-container') || createToastContainer();
  const toast = createToast(message, type);
  toastContainer.appendChild(toast);

  const bsToast = new bootstrap.Toast(toast);
  bsToast.show();

  toast.addEventListener('hidden.bs.toast', function() {
    toast.remove();
  });
}

function createToastContainer() {
  const container = document.createElement('div');
  container.className = 'toast-container position-fixed top-0 end-0 p-3';
  container.style.zIndex = '1055';
  document.body.appendChild(container);
  return container;
}

function createToast(message, type) {
  const toast = document.createElement('div');
  toast.className = `toast align-items-center text-white bg-${type} border-0`;
  toast.setAttribute('role', 'alert');
  toast.setAttribute('aria-live', 'assertive');
  toast.setAttribute('aria-atomic', 'true');

  toast.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${message}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>
  `;

  return toast;
}

// Export functions for global use
window.FireflyBuddy = {
  showToast,
  initializeCustomComponents,
  toggleTheme
};
