import hashlib
import hmac
import json
import os

from django.conf import settings
from django.core.mail import send_mail


# Loads password rules from the JSON config file
def load_password_config():
    config_path = os.path.join(settings.BASE_DIR, 'password_config.json')
    with open(config_path, 'r') as f:
        return json.load(f)


# Random 32-byte hex string, unique per user
def generate_salt():
    return os.urandom(32).hex()


# HMAC-SHA256 hash using the app's secret key + user's salt
def hash_password(password, salt):
    return hmac.new(
        settings.HMAC_SECRET_KEY.encode(),
        (password + salt).encode(),
        hashlib.sha256
    ).hexdigest()


# Compares a plaintext password against the stored hash
def verify_password(password, salt, stored_hash):
    return hash_password(password, salt) == stored_hash


# Reads the common passwords list from file into a set
def load_dictionary():
    config = load_password_config()
    dict_file = os.path.join(settings.BASE_DIR, config.get('dictionary_file', 'common_passwords.txt'))
    passwords = set()
    if os.path.exists(dict_file):
        with open(dict_file, 'r') as f:
            for line in f:
                word = line.strip().lower()
                if word:
                    passwords.add(word)
    return passwords


# Checks password against all the rules: length, complexity, dictionary, and history
def validate_password(password, user=None):
    errors = []
    config = load_password_config()

    if len(password) < config.get('min_length', 10):
        errors.append(f"Password must be at least {config.get('min_length', 10)} characters long.")

    if config.get('require_uppercase') and not any(c.isupper() for c in password):
        errors.append("Password must contain at least one uppercase letter.")

    if config.get('require_lowercase') and not any(c.islower() for c in password):
        errors.append("Password must contain at least one lowercase letter.")

    if config.get('require_digits') and not any(c.isdigit() for c in password):
        errors.append("Password must contain at least one digit.")

    if config.get('require_special'):
        special_chars = "!@#$%^&*()_+-=[]{}|;':\",./<>?"
        if not any(c in special_chars for c in password):
            errors.append("Password must contain at least one special character.")

    # Check against common passwords dictionary
    dictionary = load_dictionary()
    if password.lower() in dictionary:
        errors.append("Password is too common. Please choose a different password.")

    # Check against current password and recent history
    if user:
        from app.models import PasswordHistory
        current_hash = hash_password(password, user.salt)
        if current_hash == user.password_hash:
            errors.append("New password cannot be the same as your current password.")

        history_count = config.get('password_history_count', 3)
        history_entries = PasswordHistory.objects.filter(user=user).order_by('-created_at')[:history_count]
        for entry in history_entries:
            if hash_password(password, entry.salt) == entry.password_hash:
                errors.append("Password was used recently. Please choose a different password.")
                break

    return errors


# Creates a SHA-1 token from random bytes for password reset
def generate_reset_token():
    random_bytes = os.urandom(32)
    return hashlib.sha1(random_bytes).hexdigest()


# Sends the reset token to the user's email via Gmail SMTP
def send_reset_email(email, token):
    subject = 'Comunication_LTD - Password Reset'
    message = (
        f'You have requested a password reset.\n\n'
        f'Your reset token is: {token}\n\n'
        f'This token will expire in 15 minutes.\n\n'
        f'If you did not request this reset, please ignore this email.'
    )
    send_mail(
        subject,
        message,
        settings.EMAIL_HOST_USER,
        [email],
        fail_silently=False,
    )
