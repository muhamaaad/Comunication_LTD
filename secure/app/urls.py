# All app URL routes — maps paths to view functions
from django.urls import path
from app import views

urlpatterns = [
    path('', views.home_view, name='home'),
    path('register/', views.register_view, name='register'),
    path('login/', views.login_view, name='login'),
    path('logout/', views.logout_view, name='logout'),
    path('change-password/', views.change_password_view, name='change_password'),
    path('forgot-password/', views.forgot_password_view, name='forgot_password'),
    path('reset-token/', views.reset_token_view, name='reset_token'),
    path('system/', views.system_view, name='system'),
]
