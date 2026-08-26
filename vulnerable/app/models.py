from django.db import models


# Market categories like Residential, Business, Enterprise
class Sector(models.Model):
    name = models.CharField(max_length=100)
    description = models.TextField(blank=True)

    def __str__(self):
        return self.name


# Internet browsing packages with speed, price, and sector
class Package(models.Model):
    name = models.CharField(max_length=100)
    description = models.TextField(blank=True)
    price = models.DecimalField(max_digits=8, decimal_places=2)
    sector = models.ForeignKey(Sector, on_delete=models.CASCADE)

    def __str__(self):
        return f"{self.name} - ${self.price}"


# System users — password stored as HMAC-SHA256 hash + unique salt
class AppUser(models.Model):
    username = models.CharField(max_length=150, unique=True)
    email = models.EmailField()
    password_hash = models.CharField(max_length=256)
    salt = models.CharField(max_length=64)
    failed_attempts = models.IntegerField(default=0)
    is_locked = models.BooleanField(default=False)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return self.username


# Old password hashes — used to block reusing recent passwords
class PasswordHistory(models.Model):
    user = models.ForeignKey(AppUser, on_delete=models.CASCADE, related_name='password_history')
    password_hash = models.CharField(max_length=256)
    salt = models.CharField(max_length=64)
    created_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        ordering = ['-created_at']


# Reset tokens sent via email — expire after 15 minutes
class PasswordResetToken(models.Model):
    user = models.ForeignKey(AppUser, on_delete=models.CASCADE, related_name='reset_tokens')
    token = models.CharField(max_length=40)
    created_at = models.DateTimeField(auto_now_add=True)
    is_used = models.BooleanField(default=False)

    def is_expired(self):
        from django.utils import timezone
        import datetime
        return timezone.now() > self.created_at + datetime.timedelta(minutes=15)


# Telecom customers added through the system screen
class Customer(models.Model):
    first_name = models.CharField(max_length=100)
    last_name = models.CharField(max_length=100)
    email = models.EmailField()
    phone = models.CharField(max_length=20)
    package = models.ForeignKey(Package, on_delete=models.CASCADE)
    created_by = models.ForeignKey(AppUser, on_delete=models.CASCADE)
    created_at = models.DateTimeField(auto_now_add=True)

    def __str__(self):
        return f"{self.first_name} {self.last_name}"
