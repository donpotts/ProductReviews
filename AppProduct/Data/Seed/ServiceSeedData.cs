using AppProduct.Shared.Models;

namespace AppProduct.Data.Seed;

public static class ServiceSeedData
{
    public static List<ServiceCategory> GetServiceCategories()
    {
        return new List<ServiceCategory>
        {
            new()
            {
                Id = 1,
                Name = "IT Support",
                Description = "Technical support and troubleshooting services",
                IconClass = "fas fa-headset",
                ColorCode = "#4CAF50",
                IsActive = true,
                SortOrder = 1,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "System Consulting",
                Description = "Strategic IT consulting and architecture services",
                IconClass = "fas fa-chart-line",
                ColorCode = "#2196F3",
                IsActive = true,
                SortOrder = 2,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Name = "Onsite Maintenance",
                Description = "Physical hardware maintenance and repairs",
                IconClass = "fas fa-tools",
                ColorCode = "#FF9800",
                IsActive = true,
                SortOrder = 3,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 4,
                Name = "Software Development",
                Description = "Custom software development and programming",
                IconClass = "fas fa-code",
                ColorCode = "#9C27B0",
                IsActive = true,
                SortOrder = 4,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 5,
                Name = "Security Services",
                Description = "Cybersecurity assessment and implementation",
                IconClass = "fas fa-shield-alt",
                ColorCode = "#F44336",
                IsActive = true,
                SortOrder = 5,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 6,
                Name = "Training",
                Description = "Technical training and certification programs",
                IconClass = "fas fa-graduation-cap",
                ColorCode = "#607D8B",
                IsActive = true,
                SortOrder = 6,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };
    }

    public static List<ServiceCompany> GetServiceCompanies()
    {
        return new List<ServiceCompany>
        {
            new()
            {
                Id = 1,
                Name = "TechCorp Solutions",
                Description = "Leading provider of enterprise IT solutions and consulting services",
                ContactEmail = "contact@techcorp.com",
                ContactPhone = "(555) 123-4567",
                Website = "https://www.techcorp.com",
                Address = "123 Tech Boulevard, Silicon Valley, CA 94000",
                IsActive = true,
                IsCertified = true,
                Certifications = "ISO 27001, Microsoft Gold Partner, Cisco Premier Partner",
                YearsOfExperience = 15,
                Specialties = "Enterprise Architecture, Cloud Migration, Security",
                AverageRating = 4.8m,
                TotalReviews = 127,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "DevPro Services",
                Description = "Specialized software development and DevOps consulting",
                ContactEmail = "hello@devpro.com",
                ContactPhone = "(555) 987-6543",
                Website = "https://www.devpro.com",
                Address = "456 Developer Street, Austin, TX 73301",
                IsActive = true,
                IsCertified = true,
                Certifications = "AWS Advanced Partner, Azure Expert",
                YearsOfExperience = 8,
                Specialties = "Web Development, Mobile Apps, DevOps, Cloud",
                AverageRating = 4.6m,
                TotalReviews = 89,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Name = "SecureIT Partners",
                Description = "Cybersecurity specialists and compliance experts",
                ContactEmail = "info@secureit.com",
                ContactPhone = "(555) 456-7890",
                Website = "https://www.secureit.com",
                Address = "789 Security Lane, Washington, DC 20001",
                IsActive = true,
                IsCertified = true,
                Certifications = "CISSP, CISM, SOC 2 Type II",
                YearsOfExperience = 12,
                Specialties = "Penetration Testing, Compliance, Security Audits",
                AverageRating = 4.9m,
                TotalReviews = 156,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };
    }

    public static List<ServiceFeature> GetServiceFeatures()
    {
        return new List<ServiceFeature>
        {
            new()
            {
                Id = 1,
                Name = "24/7 Support",
                Description = "Round-the-clock technical support",
                IconClass = "fas fa-clock",
                IsActive = true,
                Type = FeatureType.Premium,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Remote Access",
                Description = "Secure remote troubleshooting capabilities",
                IconClass = "fas fa-desktop",
                IsActive = true,
                Type = FeatureType.Included,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Name = "Documentation",
                Description = "Comprehensive service documentation",
                IconClass = "fas fa-file-alt",
                IsActive = true,
                Type = FeatureType.Included,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 4,
                Name = "Training Included",
                Description = "Staff training on implemented solutions",
                IconClass = "fas fa-chalkboard-teacher",
                IsActive = true,
                Type = FeatureType.Optional,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 5,
                Name = "Warranty",
                Description = "Service warranty and guarantees",
                IconClass = "fas fa-certificate",
                IsActive = true,
                Type = FeatureType.Included,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };
    }

    public static List<ServiceTag> GetServiceTags()
    {
        return new List<ServiceTag>
        {
            new()
            {
                Id = 1,
                Name = "Urgent",
                Description = "High priority emergency services",
                ColorCode = "#F44336",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Name = "Remote",
                Description = "Services performed remotely",
                ColorCode = "#4CAF50",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Name = "Onsite",
                Description = "Services requiring physical presence",
                ColorCode = "#FF9800",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 4,
                Name = "Certified",
                Description = "Performed by certified professionals",
                ColorCode = "#2196F3",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new()
            {
                Id = 5,
                Name = "Enterprise",
                Description = "Enterprise-level services",
                ColorCode = "#9C27B0",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };
    }

    public static List<Service> GetServices()
    {
        return new List<Service>
        {
            new()
            {
                Id = 1,
                Name = "Help Desk Support",
                Description = "Basic technical support for common IT issues",
                DetailedDescription = "Our help desk support provides first-level technical assistance for common computer, software, and network issues. Includes troubleshooting, password resets, software installation guidance, and basic hardware support.",
                HourlyRate = 75.00m,
                DailyRate = null,
                ProjectRate = null,
                PricingType = ServicePricingType.Hourly,
                ServiceCategoryId = 1,
                ServiceCompanyId = 1,
                ImageUrl = "/images/services/help-desk.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 1,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Basic computer knowledge preferred",
                Deliverables = "Issue resolution, documentation of fix",
                Complexity = ServiceComplexity.Basic,
                SKU = "SRV-HD-001",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Available during business hours"
            },
            new()
            {
                Id = 2,
                Name = "Network Infrastructure Assessment",
                Description = "Comprehensive evaluation of your network infrastructure",
                DetailedDescription = "Complete assessment of your network infrastructure including performance analysis, security evaluation, capacity planning, and recommendations for improvements. Includes detailed report with findings and action plan.",
                HourlyRate = 150.00m,
                DailyRate = 1200.00m,
                ProjectRate = 8500.00m,
                PricingType = ServicePricingType.Project,
                ServiceCategoryId = 2,
                ServiceCompanyId = 1,
                ImageUrl = "/images/services/network-assessment.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 40,
                RequiresOnsite = true,
                IncludesTravel = true,
                Requirements = "Network documentation, admin access, 2-week lead time",
                Deliverables = "Assessment report, recommendations, implementation roadmap",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-NW-002",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Requires security clearance for government clients"
            },
            new()
            {
                Id = 3,
                Name = "Server Maintenance",
                Description = "Regular maintenance and monitoring of server systems",
                DetailedDescription = "Monthly onsite server maintenance including hardware inspection, software updates, performance monitoring, backup verification, and preventive maintenance tasks to ensure optimal server performance and reliability.",
                HourlyRate = 125.00m,
                DailyRate = 950.00m,
                ProjectRate = null,
                PricingType = ServicePricingType.Daily,
                ServiceCategoryId = 3,
                ServiceCompanyId = 1,
                ImageUrl = "/images/services/server-maintenance.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 8,
                RequiresOnsite = true,
                IncludesTravel = true,
                Requirements = "Admin access, maintenance window scheduling",
                Deliverables = "Maintenance report, performance metrics, recommendations",
                Complexity = ServiceComplexity.Intermediate,
                SKU = "SRV-SM-003",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Includes emergency call-out within 4 hours"
            },
            new()
            {
                Id = 4,
                Name = "Custom Web Application Development",
                Description = "Full-stack web application development services",
                DetailedDescription = "Complete custom web application development using modern frameworks and technologies. Includes requirements analysis, UI/UX design, backend development, database design, testing, deployment, and documentation.",
                HourlyRate = 185.00m,
                DailyRate = null,
                ProjectRate = null,
                PricingType = ServicePricingType.Custom,
                ServiceCategoryId = 4,
                ServiceCompanyId = 2,
                ImageUrl = "/images/services/web-development.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 320,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Detailed requirements document, design mockups preferred",
                Deliverables = "Source code, documentation, deployment guide, training",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-WD-004",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Agile methodology, bi-weekly progress reviews"
            },
            new()
            {
                Id = 5,
                Name = "Security Penetration Testing",
                Description = "Comprehensive security assessment and penetration testing",
                DetailedDescription = "Professional penetration testing service including network scanning, vulnerability assessment, exploitation attempts, and detailed security report with remediation recommendations. Conducted by certified ethical hackers.",
                HourlyRate = 200.00m,
                DailyRate = 1500.00m,
                ProjectRate = 12500.00m,
                PricingType = ServicePricingType.Project,
                ServiceCategoryId = 5,
                ServiceCompanyId = 3,
                ImageUrl = "/images/services/penetration-testing.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 80,
                RequiresOnsite = true,
                IncludesTravel = true,
                Requirements = "Written authorization, network access, test environment setup",
                Deliverables = "Penetration test report, executive summary, remediation plan",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-PT-005",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Requires NDA and proper authorization before testing"
            },
            new()
            {
                Id = 6,
                Name = "Cloud Migration Consulting",
                Description = "Strategic planning and execution of cloud migration projects",
                DetailedDescription = "End-to-end cloud migration service including current state assessment, cloud readiness evaluation, migration strategy development, cost optimization, security planning, and migration execution support.",
                HourlyRate = 175.00m,
                DailyRate = 1400.00m,
                ProjectRate = null,
                PricingType = ServicePricingType.Daily,
                ServiceCategoryId = 2,
                ServiceCompanyId = 2,
                ImageUrl = "/images/services/cloud-migration.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 160,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Current infrastructure documentation, cloud account access",
                Deliverables = "Migration plan, cost analysis, implementation timeline, training materials",
                Complexity = ServiceComplexity.Advanced,
                SKU = "SRV-CM-006",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "AWS, Azure, and Google Cloud certified consultants"
            },
            new()
            {
                Id = 7,
                Name = "IT Staff Training",
                Description = "Technical training programs for IT staff and end users",
                DetailedDescription = "Customized training programs covering various IT topics including new software systems, security best practices, network administration, and technology adoption. Can be delivered onsite or remotely.",
                HourlyRate = 95.00m,
                DailyRate = 750.00m,
                ProjectRate = null,
                PricingType = ServicePricingType.Daily,
                ServiceCategoryId = 6,
                ServiceCompanyId = 1,
                ImageUrl = "/images/services/training.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 16,
                RequiresOnsite = true,
                IncludesTravel = true,
                Requirements = "Training facility, participant list, learning objectives",
                Deliverables = "Training materials, certificates, assessment results, follow-up support",
                Complexity = ServiceComplexity.Intermediate,
                SKU = "SRV-TR-007",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Maximum 20 participants per session for optimal learning"
            },
            new()
            {
                Id = 8,
                Name = "Database Design & Development",
                Description = "Custom database solutions for your business needs",
                DetailedDescription = "Complete database design and development services including requirements analysis, entity relationship modeling, database architecture, optimization, stored procedures, and data migration services.",
                HourlyRate = 165.00m,
                DailyRate = null,
                ProjectRate = null,
                PricingType = ServicePricingType.Hourly,
                ServiceCategoryId = 4,
                ServiceCompanyId = 2,
                ImageUrl = "/images/services/database-design.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 80,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Database requirements document, existing data inventory",
                Deliverables = "Database schema, documentation, migration scripts, user guides",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-DB-008",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Supports MySQL, PostgreSQL, SQL Server, and Oracle databases"
            },
            new()
            {
                Id = 9,
                Name = "Mobile App Development",
                Description = "Native and cross-platform mobile application development",
                DetailedDescription = "Professional mobile app development for iOS and Android platforms using React Native, Flutter, or native development. Includes UI/UX design, backend integration, testing, and app store deployment.",
                HourlyRate = 195.00m,
                DailyRate = null,
                ProjectRate = null,
                PricingType = ServicePricingType.Custom,
                ServiceCategoryId = 4,
                ServiceCompanyId = 2,
                ImageUrl = "/images/services/mobile-development.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 480,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "App wireframes, functional requirements, target platform specifications",
                Deliverables = "Mobile app, source code, app store listing, user documentation",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-MA-009",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "App store submission and review support included"
            },
            new()
            {
                Id = 10,
                Name = "Digital Marketing Strategy",
                Description = "Comprehensive digital marketing planning and execution",
                DetailedDescription = "Full digital marketing strategy development including SEO analysis, social media planning, content marketing, email campaigns, and performance analytics setup to boost your online presence.",
                HourlyRate = 135.00m,
                DailyRate = 1080.00m,
                ProjectRate = null,
                PricingType = ServicePricingType.Daily,
                ServiceCategoryId = 6,
                ServiceCompanyId = 3,
                ImageUrl = "/images/services/digital-marketing.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 32,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Current marketing materials, website access, business goals",
                Deliverables = "Marketing strategy document, content calendar, analytics setup",
                Complexity = ServiceComplexity.Intermediate,
                SKU = "SRV-DM-010",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "3-month strategy implementation support included"
            },
            new()
            {
                Id = 11,
                Name = "Disaster Recovery Planning",
                Description = "Business continuity and disaster recovery planning services",
                DetailedDescription = "Comprehensive disaster recovery planning including risk assessment, backup strategy development, recovery procedures, testing protocols, and staff training to ensure business continuity.",
                HourlyRate = 180.00m,
                DailyRate = 1440.00m,
                ProjectRate = 15000.00m,
                PricingType = ServicePricingType.Project,
                ServiceCategoryId = 5,
                ServiceCompanyId = 1,
                ImageUrl = "/images/services/disaster-recovery.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 100,
                RequiresOnsite = true,
                IncludesTravel = true,
                Requirements = "Current infrastructure documentation, business impact analysis",
                Deliverables = "DR plan, backup procedures, testing schedule, training materials",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-DR-011",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Annual plan review and update included"
            },
            new()
            {
                Id = 12,
                Name = "E-commerce Platform Setup",
                Description = "Complete e-commerce website development and configuration",
                DetailedDescription = "End-to-end e-commerce platform setup including platform selection, custom design, payment gateway integration, inventory management, shipping configuration, and SEO optimization.",
                HourlyRate = 155.00m,
                DailyRate = null,
                ProjectRate = 18500.00m,
                PricingType = ServicePricingType.Project,
                ServiceCategoryId = 4,
                ServiceCompanyId = 2,
                ImageUrl = "/images/services/ecommerce-setup.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 120,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Product catalog, payment processing requirements, hosting preferences",
                Deliverables = "E-commerce website, admin training, documentation, launch support",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-EC-012",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "3 months post-launch support included"
            },
            new()
            {
                Id = 13,
                Name = "IT Asset Management",
                Description = "Comprehensive IT asset tracking and management services",
                DetailedDescription = "Complete IT asset management including inventory tracking, lifecycle management, maintenance scheduling, license compliance, and disposal services for all your technology assets.",
                HourlyRate = 95.00m,
                DailyRate = 760.00m,
                ProjectRate = null,
                PricingType = ServicePricingType.Daily,
                ServiceCategoryId = 3,
                ServiceCompanyId = 1,
                ImageUrl = "/images/services/asset-management.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 24,
                RequiresOnsite = true,
                IncludesTravel = true,
                Requirements = "Current asset inventory, procurement records, license documentation",
                Deliverables = "Asset management system, documentation, compliance reports",
                Complexity = ServiceComplexity.Intermediate,
                SKU = "SRV-AM-013",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Quarterly asset audits available as add-on service"
            },
            new()
            {
                Id = 14,
                Name = "API Development & Integration",
                Description = "Custom API development and third-party system integration",
                DetailedDescription = "Professional API development services including RESTful API design, microservices architecture, third-party integrations, API documentation, and testing to connect your systems seamlessly.",
                HourlyRate = 175.00m,
                DailyRate = null,
                ProjectRate = null,
                PricingType = ServicePricingType.Hourly,
                ServiceCategoryId = 4,
                ServiceCompanyId = 2,
                ImageUrl = "/images/services/api-development.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 60,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Integration requirements, API specifications, system documentation",
                Deliverables = "API endpoints, documentation, integration code, testing suite",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-API-014",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Real-time API monitoring and support available"
            },
            new()
            {
                Id = 15,
                Name = "Data Analytics & Visualization",
                Description = "Business intelligence and data visualization solutions",
                DetailedDescription = "Comprehensive data analytics services including data warehouse design, ETL processes, dashboard creation, report automation, and predictive analytics to drive data-driven decision making.",
                HourlyRate = 165.00m,
                DailyRate = 1320.00m,
                ProjectRate = null,
                PricingType = ServicePricingType.Daily,
                ServiceCategoryId = 4,
                ServiceCompanyId = 3,
                ImageUrl = "/images/services/data-analytics.jpg",
                IsAvailable = true,
                EstimatedDurationHours = 40,
                RequiresOnsite = false,
                IncludesTravel = false,
                Requirements = "Data sources, business metrics, reporting requirements",
                Deliverables = "Analytics dashboard, reports, data models, user training",
                Complexity = ServiceComplexity.Expert,
                SKU = "SRV-DA-015",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Notes = "Supports Tableau, Power BI, and custom visualization solutions"
            }
        };
    }

    public static List<ServiceReview> GetServiceReviews()
    {
        return new List<ServiceReview>
        {
            new()
            {
                Id = 1,
                ServiceId = 1, // Help Desk Support
                CustomerName = "John Smith",
                CustomerEmail = "john.smith@email.com",
                Rating = 4.5m,
                Title = "Great support experience",
                ReviewText = "The help desk team was very responsive and resolved my issue quickly. Professional service and clear communication throughout the process.",
                ReviewDate = DateTime.UtcNow.AddDays(-15),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 4m,
                CommunicationRating = 5m,
                ValueRating = 4m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Support,
                ProjectType = "IT Support",
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                ModifiedDate = DateTime.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = 2,
                ServiceId = 2, // Network Infrastructure Assessment
                CustomerName = "Sarah Johnson",
                CustomerEmail = "sarah.johnson@company.com",
                Rating = 5.0m,
                Title = "Comprehensive and professional assessment",
                ReviewText = "Exceptional work! The team conducted a thorough assessment of our network infrastructure and provided detailed recommendations. Their expertise saved us from potential security vulnerabilities.",
                ReviewDate = DateTime.UtcNow.AddDays(-8),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 5m,
                CommunicationRating = 5m,
                ValueRating = 5m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Consulting,
                ProjectType = "Network Assessment",
                CreatedDate = DateTime.UtcNow.AddDays(-8),
                ModifiedDate = DateTime.UtcNow.AddDays(-8)
            },
            new()
            {
                Id = 3,
                ServiceId = 4, // Custom Web Application Development
                CustomerName = "Mike Wilson",
                CustomerEmail = "mike.wilson@startup.com",
                Rating = 4.8m,
                Title = "Excellent development team",
                ReviewText = "The development team delivered exactly what we needed. Clean code, good documentation, and they met all our deadlines. Highly recommend for web development projects.",
                ReviewDate = DateTime.UtcNow.AddDays(-25),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 5m,
                CommunicationRating = 4m,
                ValueRating = 5m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Implementation,
                ProjectType = "Web Application",
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                ModifiedDate = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = 4,
                ServiceId = 5, // Security Penetration Testing
                CustomerName = "Lisa Chen",
                CustomerEmail = "lisa.chen@enterprise.com",
                Rating = 4.9m,
                Title = "Critical security insights",
                ReviewText = "The penetration testing revealed several critical vulnerabilities we weren't aware of. The report was detailed and actionable. Professional team with excellent security expertise.",
                ReviewDate = DateTime.UtcNow.AddDays(-12),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 5m,
                CommunicationRating = 5m,
                ValueRating = 4m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Consulting,
                ProjectType = "Security Assessment",
                CreatedDate = DateTime.UtcNow.AddDays(-12),
                ModifiedDate = DateTime.UtcNow.AddDays(-12)
            },
            new()
            {
                Id = 5,
                ServiceId = 7, // IT Staff Training
                CustomerName = "Robert Davis",
                CustomerEmail = "robert.davis@corp.com",
                Rating = 4.2m,
                Title = "Good training session",
                ReviewText = "The training was informative and well-structured. Our team learned valuable skills. The instructor was knowledgeable, though the pace could have been slightly slower for some topics.",
                ReviewDate = DateTime.UtcNow.AddDays(-5),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 4m,
                TimelinessRating = 4m,
                CommunicationRating = 4m,
                ValueRating = 4m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Training,
                ProjectType = "Staff Training",
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                ModifiedDate = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = 6,
                ServiceId = 8, // Database Design & Development
                CustomerName = "Robert Kim",
                CustomerEmail = "robert.kim@datatech.com",
                Rating = 4.8m,
                Title = "Excellent database architecture",
                ReviewText = "The team delivered a well-designed database with excellent performance optimization. Their expertise in data modeling saved us months of development time.",
                ReviewDate = DateTime.UtcNow.AddDays(-18),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 4m,
                CommunicationRating = 5m,
                ValueRating = 5m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Implementation,
                ProjectType = "Database Development",
                CreatedDate = DateTime.UtcNow.AddDays(-18),
                ModifiedDate = DateTime.UtcNow.AddDays(-18)
            },
            new()
            {
                Id = 7,
                ServiceId = 9, // Mobile App Development
                CustomerName = "Jennifer Lopez",
                CustomerEmail = "jennifer.lopez@startup.com",
                Rating = 5.0m,
                Title = "Outstanding mobile app development",
                ReviewText = "Amazing work! The team developed our mobile app faster than expected with exceptional quality. The app store approval went smoothly and user feedback has been fantastic.",
                ReviewDate = DateTime.UtcNow.AddDays(-25),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 5m,
                CommunicationRating = 5m,
                ValueRating = 5m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Implementation,
                ProjectType = "Mobile App",
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                ModifiedDate = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = 8,
                ServiceId = 10, // Digital Marketing Strategy
                CustomerName = "Tom Wilson",
                CustomerEmail = "tom.wilson@retailstore.com",
                Rating = 4.6m,
                Title = "Comprehensive marketing strategy",
                ReviewText = "Great strategic approach to our digital marketing. The team provided detailed analytics and actionable recommendations that significantly improved our online presence.",
                ReviewDate = DateTime.UtcNow.AddDays(-30),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 4m,
                CommunicationRating = 5m,
                ValueRating = 4m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Consulting,
                ProjectType = "Marketing Strategy",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                ModifiedDate = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = 9,
                ServiceId = 12, // E-commerce Platform Setup
                CustomerName = "Amanda Foster",
                CustomerEmail = "amanda.foster@onlineshop.com",
                Rating = 4.9m,
                Title = "Perfect e-commerce solution",
                ReviewText = "The e-commerce platform exceeded our expectations. Professional setup, seamless payment integration, and excellent training. Our online sales increased by 300% in the first month!",
                ReviewDate = DateTime.UtcNow.AddDays(-40),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 5m,
                CommunicationRating = 5m,
                ValueRating = 5m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Implementation,
                ProjectType = "E-commerce Platform",
                CreatedDate = DateTime.UtcNow.AddDays(-40),
                ModifiedDate = DateTime.UtcNow.AddDays(-40)
            },
            new()
            {
                Id = 10,
                ServiceId = 15, // Data Analytics & Visualization
                CustomerName = "Daniel Rodriguez",
                CustomerEmail = "daniel.rodriguez@analytics.com",
                Rating = 4.7m,
                Title = "Powerful analytics dashboard",
                ReviewText = "The analytics dashboard transformed how we view our business data. Clear visualizations and automated reports save us hours of manual work every week.",
                ReviewDate = DateTime.UtcNow.AddDays(-35),
                IsVerifiedCustomer = true,
                IsApproved = true,
                QualityRating = 5m,
                TimelinessRating = 4m,
                CommunicationRating = 5m,
                ValueRating = 5m,
                WouldRecommend = true,
                ReviewCategory = ServiceReviewCategory.Consulting,
                ProjectType = "Data Analytics",
                CreatedDate = DateTime.UtcNow.AddDays(-35),
                ModifiedDate = DateTime.UtcNow.AddDays(-35)
            }
        };
    }

    public static List<ServiceOrder> GetServiceOrders()
    {
        return new List<ServiceOrder>
        {
            new()
            {
                Id = 1,
                OrderNumber = "SO-2024-001",
                UserId = "user1@example.com",
                OrderDate = DateTime.UtcNow.AddDays(-30),
                Status = ServiceOrderStatus.Completed,
                Subtotal = 2400.00m,
                TaxAmount = 192.00m,
                ExpenseAmount = 150.00m,
                TotalAmount = 2742.00m,
                PaymentMethod = PaymentMethod.CreditCard,
                RequiresOnsite = true,
                OnsiteAddress = "123 Business Ave, City, State 12345",
                ContactPerson = "John Smith",
                ContactPhone = "(555) 123-4567",
                ContactEmail = "john.smith@email.com",
                ScheduledStartDate = DateTime.UtcNow.AddDays(-25),
                ScheduledEndDate = DateTime.UtcNow.AddDays(-23),
                ActualStartDate = DateTime.UtcNow.AddDays(-25),
                ActualEndDate = DateTime.UtcNow.AddDays(-22),
                CompletionNotes = "Network assessment completed successfully. All recommendations documented and delivered.",
                CustomerSignature = "J.Smith",
                SignatureDate = DateTime.UtcNow.AddDays(-22)
            },
            new()
            {
                Id = 2,
                OrderNumber = "SO-2024-002",
                UserId = "user2@example.com",
                OrderDate = DateTime.UtcNow.AddDays(-20),
                Status = ServiceOrderStatus.InProgress,
                Subtotal = 1200.00m,
                TaxAmount = 96.00m,
                ExpenseAmount = 75.00m,
                TotalAmount = 1371.00m,
                PaymentMethod = PaymentMethod.PurchaseOrder,
                RequiresOnsite = true,
                OnsiteAddress = "456 Corporate Blvd, City, State 12345",
                ContactPerson = "Sarah Johnson",
                ContactPhone = "(555) 987-6543",
                ContactEmail = "sarah.johnson@company.com",
                ScheduledStartDate = DateTime.UtcNow.AddDays(-15),
                ScheduledEndDate = DateTime.UtcNow.AddDays(-10),
                ActualStartDate = DateTime.UtcNow.AddDays(-15),
                Notes = "Server maintenance in progress. Regular monthly service."
            },
            new()
            {
                Id = 3,
                OrderNumber = "SO-2024-003",
                UserId = "user3@example.com",
                OrderDate = DateTime.UtcNow.AddDays(-10),
                Status = ServiceOrderStatus.Scheduled,
                Subtotal = 3200.00m,
                TaxAmount = 256.00m,
                ExpenseAmount = 0.00m,
                TotalAmount = 3456.00m,
                PaymentMethod = PaymentMethod.CreditCard,
                RequiresOnsite = false,
                ContactPerson = "Mike Wilson",
                ContactPhone = "(555) 456-7890",
                ContactEmail = "mike.wilson@startup.com",
                ScheduledStartDate = DateTime.UtcNow.AddDays(5),
                ScheduledEndDate = DateTime.UtcNow.AddDays(15),
                Notes = "Custom web application development project. Remote work arrangement."
            }
        };
    }

    public static List<ServiceExpense> GetServiceExpenses()
    {
        return new List<ServiceExpense>
        {
            new()
            {
                Id = 1,
                ServiceOrderId = 1,
                ExpenseType = ServiceExpenseType.Meals,
                Description = "Team lunch during onsite assessment",
                Amount = 85.50m,
                ExpenseDate = DateTime.UtcNow.AddDays(-24),
                ApprovalStatus = ServiceExpenseStatus.Approved,
                ApprovedDate = DateTime.UtcNow.AddDays(-23),
                ApprovedBy = "Manager",
                IsReimbursable = true,
                Vendor = "Local Restaurant",
                Location = "Client Site",
                CreatedDate = DateTime.UtcNow.AddDays(-24),
                ModifiedDate = DateTime.UtcNow.AddDays(-23)
            },
            new()
            {
                Id = 2,
                ServiceOrderId = 1,
                ExpenseType = ServiceExpenseType.LocalTransport,
                Description = "Taxi fare to client location",
                Amount = 32.75m,
                ExpenseDate = DateTime.UtcNow.AddDays(-25),
                ApprovalStatus = ServiceExpenseStatus.Approved,
                ApprovedDate = DateTime.UtcNow.AddDays(-23),
                ApprovedBy = "Manager",
                IsReimbursable = true,
                Vendor = "City Taxi",
                Location = "Client Site",
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                ModifiedDate = DateTime.UtcNow.AddDays(-23)
            },
            new()
            {
                Id = 3,
                ServiceOrderId = 1,
                ExpenseType = ServiceExpenseType.Materials,
                Description = "Network testing equipment",
                Amount = 125.00m,
                ExpenseDate = DateTime.UtcNow.AddDays(-26),
                ApprovalStatus = ServiceExpenseStatus.Approved,
                ApprovedDate = DateTime.UtcNow.AddDays(-25),
                ApprovedBy = "Manager",
                IsReimbursable = true,
                Vendor = "Tech Supply Co",
                Location = "Office",
                CreatedDate = DateTime.UtcNow.AddDays(-26),
                ModifiedDate = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = 4,
                ServiceOrderId = 2,
                ExpenseType = ServiceExpenseType.Parking,
                Description = "Parking at client building",
                Amount = 15.00m,
                ExpenseDate = DateTime.UtcNow.AddDays(-15),
                ApprovalStatus = ServiceExpenseStatus.Pending,
                IsReimbursable = true,
                Vendor = "Client Building Parking",
                Location = "Client Site",
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                ModifiedDate = DateTime.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = 5,
                ServiceOrderId = 2,
                ExpenseType = ServiceExpenseType.Meals,
                Description = "Working lunch during server maintenance",
                Amount = 28.50m,
                ExpenseDate = DateTime.UtcNow.AddDays(-14),
                ApprovalStatus = ServiceExpenseStatus.Pending,
                IsReimbursable = true,
                Vendor = "Sandwich Shop",
                Location = "Client Site",
                CreatedDate = DateTime.UtcNow.AddDays(-14),
                ModifiedDate = DateTime.UtcNow.AddDays(-14)
            }
        };
    }
}