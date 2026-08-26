import html

from django.shortcuts import render, redirect
from django.contrib import messages

from app.models import AppUser, PasswordHistory, PasswordResetToken, Customer, Package
from app.forms import (
    RegisterForm, LoginForm, ChangePasswordForm,
    ForgotPasswordForm, ResetTokenForm, CustomerForm
)
from app.utils import (
    generate_salt, hash_password, verify_password,
    validate_password, generate_reset_token, send_reset_email,
    load_password_config
)


# Redirect to system if logged in, otherwise to login
def home_view(request):
    if request.session.get('user_id'):
        return redirect('system')
    return redirect('login')


# Handles new user registration with password validation
def register_view(request):
    if request.method == 'POST':
        form = RegisterForm(request.POST)
        if form.is_valid():
            # SECURE: html.escape() encodes special characters like ' " < > &
            # For example: O'Brien becomes O&#x27;Brien, <script> becomes &lt;script&gt;
            # This prevents both SQL injection and XSS attacks
            username = html.escape(form.cleaned_data['username'])
            email = html.escape(form.cleaned_data['email'])
            password = form.cleaned_data['password']
            confirm_password = form.cleaned_data['confirm_password']

            errors = []

            if password != confirm_password:
                errors.append("Passwords do not match.")

            password_errors = validate_password(password)
            errors.extend(password_errors)

            if AppUser.objects.filter(username=username).exists():
                errors.append("Username already exists.")

            if errors:
                return render(request, 'app/register.html', {
                    'form': form, 'errors': errors
                })

            salt = generate_salt()
            password_hash = hash_password(password, salt)

            # SECURE: Django ORM uses parameterized queries (prepared statements)
            # Equivalent to: INSERT INTO ... VALUES (%s, %s, %s, ...) with bound parameters
            # Even if encoding is bypassed, parameterized queries prevent SQL injection
            user = AppUser.objects.create(
                username=username,
                email=email,
                password_hash=password_hash,
                salt=salt
            )

            PasswordHistory.objects.create(
                user=user,
                password_hash=password_hash,
                salt=salt
            )

            messages.success(request, "Registration successful! Please login.")
            return redirect('login')
    else:
        form = RegisterForm()

    return render(request, 'app/register.html', {'form': form})


# Handles login with failed attempt tracking and account lockout
def login_view(request):
    if request.method == 'POST':
        form = LoginForm(request.POST)
        if form.is_valid():
            # SECURE: html.escape() encodes special characters like ' " < > &
            # This prevents malicious characters from being interpreted as code
            username = html.escape(form.cleaned_data['username'])
            password = form.cleaned_data['password']
            config = load_password_config()
            max_attempts = config.get('max_login_attempts', 3)

            # SECURE: Django ORM uses parameterized queries internally
            # The query becomes: SELECT ... WHERE username=%s with ['username'] as parameter
            # Even if html.escape() is bypassed, parameterized queries prevent SQL injection
            try:
                user = AppUser.objects.get(username=username)
            except AppUser.DoesNotExist:
                return render(request, 'app/login.html', {
                    'form': form,
                    'errors': ['Invalid username or password.']
                })

            if user.is_locked:
                return render(request, 'app/locked.html')

            if verify_password(password, user.salt, user.password_hash):
                user.failed_attempts = 0
                user.save()
                request.session['user_id'] = user.id
                request.session['username'] = user.username
                return redirect('system')
            else:
                user.failed_attempts += 1
                if user.failed_attempts >= max_attempts:
                    user.is_locked = True
                user.save()

                if user.is_locked:
                    return render(request, 'app/locked.html')

                remaining = max_attempts - user.failed_attempts
                return render(request, 'app/login.html', {
                    'form': form,
                    'errors': [f'Invalid username or password. {remaining} attempt(s) remaining.']
                })
    else:
        form = LoginForm()

    return render(request, 'app/login.html', {'form': form})


# Destroys session and redirects to login
def logout_view(request):
    request.session.flush()
    messages.success(request, "You have been logged out.")
    return redirect('login')


# Handles both normal password change and forgot-password reset flow
def change_password_view(request):
    user_id = request.session.get('user_id')
    reset_user_id = request.session.get('reset_user_id')
    is_reset = False

    if user_id:
        try:
            user = AppUser.objects.get(id=user_id)
        except AppUser.DoesNotExist:
            return redirect('login')
    elif reset_user_id:
        try:
            user = AppUser.objects.get(id=reset_user_id)
            is_reset = True
        except AppUser.DoesNotExist:
            return redirect('login')
    else:
        return redirect('login')

    if request.method == 'POST':
        form = ChangePasswordForm(request.POST)
        if form.is_valid():
            new_password = form.cleaned_data['new_password']
            confirm_new_password = form.cleaned_data['confirm_new_password']

            errors = []

            # Skip current password check if user came through forgot-password
            if not is_reset:
                current_password = form.cleaned_data['current_password']
                if not verify_password(current_password, user.salt, user.password_hash):
                    errors.append("Current password is incorrect.")

            if new_password != confirm_new_password:
                errors.append("New passwords do not match.")

            if not errors:
                password_errors = validate_password(new_password, user=user)
                errors.extend(password_errors)

            if errors:
                return render(request, 'app/change_password.html', {
                    'form': form, 'errors': errors, 'user_id': user_id, 'is_reset': is_reset
                })

            salt = generate_salt()
            password_hash = hash_password(new_password, salt)

            # Save old password to history before updating
            PasswordHistory.objects.create(
                user=user,
                password_hash=user.password_hash,
                salt=user.salt
            )

            user.password_hash = password_hash
            user.salt = salt
            # Unlock account if this was a forgot-password reset
            if is_reset:
                user.is_locked = False
                user.failed_attempts = 0
            user.save()

            # Trim password history to configured limit
            config = load_password_config()
            history_count = config.get('password_history_count', 3)
            old_entries = PasswordHistory.objects.filter(user=user).order_by('-created_at')[history_count:]
            for entry in old_entries:
                entry.delete()

            if is_reset:
                del request.session['reset_user_id']
                messages.success(request, 'Password reset successfully! Please login.')
                return redirect('login')

            return render(request, 'app/change_password.html', {
                'form': ChangePasswordForm(),
                'success': 'Password changed successfully!',
                'user_id': user_id
            })
    else:
        form = ChangePasswordForm()

    return render(request, 'app/change_password.html', {
        'form': form, 'user_id': user_id, 'is_reset': is_reset
    })


# Generates a reset token and emails it to the user
def forgot_password_view(request):
    if request.method == 'POST':
        form = ForgotPasswordForm(request.POST)
        if form.is_valid():
            email = form.cleaned_data['email']

            try:
                user = AppUser.objects.get(email=email)
                token = generate_reset_token()

                PasswordResetToken.objects.create(
                    user=user,
                    token=token
                )

                send_reset_email(email, token)
            except AppUser.DoesNotExist:
                pass

            # Same message whether email exists or not (prevents enumeration)
            return render(request, 'app/forgot_password.html', {
                'form': ForgotPasswordForm(),
                'success': 'If an account with that email exists, a reset token has been sent.'
            })
    else:
        form = ForgotPasswordForm()

    return render(request, 'app/forgot_password.html', {'form': form})


# Verifies the reset token and redirects to password change
def reset_token_view(request):
    if request.method == 'POST':
        form = ResetTokenForm(request.POST)
        if form.is_valid():
            token = form.cleaned_data['token']

            try:
                reset_token = PasswordResetToken.objects.get(
                    token=token,
                    is_used=False
                )

                if reset_token.is_expired():
                    return render(request, 'app/reset_token.html', {
                        'form': form,
                        'errors': ['This token has expired. Please request a new one.']
                    })

                reset_token.is_used = True
                reset_token.save()

                request.session['reset_user_id'] = reset_token.user.id
                return redirect('change_password')

            except PasswordResetToken.DoesNotExist:
                return render(request, 'app/reset_token.html', {
                    'form': form,
                    'errors': ['Invalid token.']
                })
    else:
        form = ResetTokenForm()

    return render(request, 'app/reset_token.html', {'form': form})


# Main system screen — add customers and display them
def system_view(request):
    user_id = request.session.get('user_id')
    if not user_id:
        return redirect('login')

    try:
        user = AppUser.objects.get(id=user_id)
    except AppUser.DoesNotExist:
        return redirect('login')

    customer = None

    if request.method == 'POST':
        form = CustomerForm(request.POST)
        if form.is_valid():
            first_name = form.cleaned_data['first_name']
            last_name = form.cleaned_data['last_name']
            email = form.cleaned_data['email']
            phone = form.cleaned_data['phone']
            package = form.cleaned_data['package']

            # SECURE: html.escape() encodes special characters to prevent XSS and injection
            first_name = html.escape(first_name)
            last_name = html.escape(last_name)
            email = html.escape(email)

            # SECURE: Django ORM uses parameterized queries (prepared statements)
            # Equivalent to: INSERT INTO ... VALUES (%s, %s, %s, ...) with escaped parameters
            # This prevents SQL injection even if special characters are in the input
            new_customer = Customer.objects.create(
                first_name=first_name,
                last_name=last_name,
                email=email,
                phone=phone,
                package=package,
                created_by=user
            )

            customer = {
                'first_name': new_customer.first_name,
                'last_name': new_customer.last_name,
                'email': new_customer.email,
                'phone': new_customer.phone,
                'package_name': str(new_customer.package),
            }

            form = CustomerForm()
    else:
        form = CustomerForm()

    return render(request, 'app/system.html', {
        'form': form,
        'customer': customer,
        'user_id': user_id,
        'username': user.username
    })

