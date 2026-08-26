from django.core.management.base import BaseCommand
from app.models import Sector, Package


class Command(BaseCommand):
    help = 'Seed the database with initial sectors and packages'

    def handle(self, *args, **options):
        # Create the three market sectors
        residential, _ = Sector.objects.get_or_create(
            name='Residential',
            defaults={'description': 'Home and personal use internet packages'}
        )
        business, _ = Sector.objects.get_or_create(
            name='Business',
            defaults={'description': 'Business and commercial internet packages'}
        )
        enterprise, _ = Sector.objects.get_or_create(
            name='Enterprise',
            defaults={'description': 'Enterprise-grade high-performance packages'}
        )

        # Create the five browsing packages
        packages = [
            {'name': 'Basic 50Mbps', 'description': 'Basic browsing package for light users', 'price': 29.99, 'sector': residential},
            {'name': 'Standard 100Mbps', 'description': 'Standard package for everyday browsing', 'price': 49.99, 'sector': residential},
            {'name': 'Premium 200Mbps', 'description': 'Premium package for heavy users and streaming', 'price': 79.99, 'sector': business},
            {'name': 'Ultra 500Mbps', 'description': 'Ultra-fast package for businesses', 'price': 129.99, 'sector': business},
            {'name': 'Enterprise 1Gbps', 'description': 'Maximum speed enterprise solution', 'price': 249.99, 'sector': enterprise},
        ]

        for pkg_data in packages:
            Package.objects.get_or_create(
                name=pkg_data['name'],
                defaults={
                    'description': pkg_data['description'],
                    'price': pkg_data['price'],
                    'sector': pkg_data['sector'],
                }
            )

        self.stdout.write(self.style.SUCCESS('Successfully seeded database with sectors and packages'))
