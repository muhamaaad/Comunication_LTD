from django import forms
from app.models import Package


# New user signup form
class RegisterForm(forms.Form):
    # VULNERABLE: strip=False allows trailing spaces in input
    username = forms.CharField(max_length=150, strip=False, widget=forms.TextInput(attrs={
        'class': 'form-control', 'placeholder': 'Username'
    }))
    email = forms.EmailField(widget=forms.EmailInput(attrs={
        'class': 'form-control', 'placeholder': 'Email'
    }))
    password = forms.CharField(widget=forms.PasswordInput(attrs={
        'class': 'form-control', 'placeholder': 'Password'
    }))
    confirm_password = forms.CharField(widget=forms.PasswordInput(attrs={
        'class': 'form-control', 'placeholder': 'Confirm Password'
    }))


# Username + password for logging in
class LoginForm(forms.Form):
    # VULNERABLE: strip=False allows trailing spaces in input (needed for SQL comment syntax --)
    username = forms.CharField(max_length=150, strip=False, widget=forms.TextInput(attrs={
        'class': 'form-control', 'placeholder': 'Username'
    }))
    password = forms.CharField(widget=forms.PasswordInput(attrs={
        'class': 'form-control', 'placeholder': 'Password'
    }))


# Password change — current_password is optional (not needed for reset flow)
class ChangePasswordForm(forms.Form):
    current_password = forms.CharField(required=False, widget=forms.PasswordInput(attrs={
        'class': 'form-control', 'placeholder': 'Current Password'
    }))
    new_password = forms.CharField(widget=forms.PasswordInput(attrs={
        'class': 'form-control', 'placeholder': 'New Password'
    }))
    confirm_new_password = forms.CharField(widget=forms.PasswordInput(attrs={
        'class': 'form-control', 'placeholder': 'Confirm New Password'
    }))


# Email input for requesting a password reset token
class ForgotPasswordForm(forms.Form):
    email = forms.EmailField(widget=forms.EmailInput(attrs={
        'class': 'form-control', 'placeholder': 'Enter your email'
    }))


# Token input for verifying a password reset
class ResetTokenForm(forms.Form):
    token = forms.CharField(max_length=40, widget=forms.TextInput(attrs={
        'class': 'form-control', 'placeholder': 'Enter reset token'
    }))


# Add a new telecom customer with a browsing package
class CustomerForm(forms.Form):
    # VULNERABLE: strip=False allows trailing spaces in input
    first_name = forms.CharField(max_length=100, strip=False, widget=forms.TextInput(attrs={
        'class': 'form-control', 'placeholder': 'First Name'
    }))
    last_name = forms.CharField(max_length=100, strip=False, widget=forms.TextInput(attrs={
        'class': 'form-control', 'placeholder': 'Last Name'
    }))
    email = forms.EmailField(widget=forms.EmailInput(attrs={
        'class': 'form-control', 'placeholder': 'Email'
    }))
    phone = forms.CharField(max_length=20, widget=forms.TextInput(attrs={
        'class': 'form-control', 'placeholder': 'Phone'
    }))
    package = forms.ModelChoiceField(
        queryset=Package.objects.all(),
        widget=forms.Select(attrs={'class': 'form-control'}),
        empty_label='Select a package'
    )
