"""
Django settings for comunication_ltd project (VULNERABLE version).
"""

from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

SECRET_KEY = 'django-insecure-jfwwc)arg7k^bnlf5&)5)*n&7xj)^wo57gszqs9p=f9e-(w8=8'

DEBUG = True

ALLOWED_HOSTS = ['*']

INSTALLED_APPS = [
    'django.contrib.admin',
    'django.contrib.auth',
    'django.contrib.contenttypes',
    'django.contrib.sessions',
    'django.contrib.messages',
    'django.contrib.staticfiles',
    'app',
]

MIDDLEWARE = [
    'django.middleware.security.SecurityMiddleware',
    'django.contrib.sessions.middleware.SessionMiddleware',
    'django.middleware.common.CommonMiddleware',
    'django.middleware.csrf.CsrfViewMiddleware',
    'django.contrib.auth.middleware.AuthenticationMiddleware',
    'django.contrib.messages.middleware.MessageMiddleware',
    'django.middleware.clickjacking.XFrameOptionsMiddleware',
]

ROOT_URLCONF = 'comunication_ltd.urls'

TEMPLATES = [
    {
        'BACKEND': 'django.template.backends.django.DjangoTemplates',
        'DIRS': [],
        'APP_DIRS': True,
        'OPTIONS': {
            'context_processors': [
                'django.template.context_processors.request',
                'django.contrib.auth.context_processors.auth',
                'django.contrib.messages.context_processors.messages',
            ],
        },
    },
]

WSGI_APPLICATION = 'comunication_ltd.wsgi.application'

DATABASES = {
    'default': {
        'ENGINE': 'django.db.backends.mysql',
        'NAME': 'comunication_ltd_vulnerable',
        'USER': 'root',
        'PASSWORD': 'BenjiHIT2026!#$%',
        'HOST': 'localhost',
        'PORT': '3306',
    }
}

AUTH_PASSWORD_VALIDATORS = []

LANGUAGE_CODE = 'en-us'
TIME_ZONE = 'UTC'
USE_I18N = True
USE_TZ = True

STATIC_URL = 'static/'
DEFAULT_AUTO_FIELD = 'django.db.models.BigAutoField'

# Used as the key for HMAC-SHA256 password hashing
HMAC_SECRET_KEY = 'comunication-ltd-hmac-secret-key-2024-xK9mP2vL'

# Gmail SMTP for sending password reset emails
EMAIL_BACKEND = 'django.core.mail.backends.smtp.EmailBackend'
EMAIL_HOST = 'smtp.gmail.com'
EMAIL_PORT = 587
EMAIL_USE_TLS = True
EMAIL_HOST_USER = 'benji.ender@gmail.com'
EMAIL_HOST_PASSWORD = 'lhkwspfxibooplez'
DEFAULT_FROM_EMAIL = 'benji.ender@gmail.com'

# Sessions expire after 30 min of inactivity, reset on each request
SESSION_COOKIE_AGE = 1800
SESSION_EXPIRE_AT_BROWSER_CLOSE = True
SESSION_SAVE_EVERY_REQUEST = True
SESSION_ENGINE = 'django.contrib.sessions.backends.db'

# Allow pages to be loaded in iframes from the same origin (for attack POC demos)
X_FRAME_OPTIONS = 'SAMEORIGIN'

# Unique cookie names so vulnerable and secure apps don't clash on localhost
SESSION_COOKIE_NAME = 'sessionid_vulnerable'
CSRF_COOKIE_NAME = 'csrftoken_vulnerable'
