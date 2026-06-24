# Application Catalog

## Overview

The Application Catalog is the central registry of all internal software applications. It provides a searchable, filterable list of applications along with their metadata, environment URLs, and contact information.

## Browsing Applications

You can browse the application catalog to find information about any registered application. The list can be filtered by:

- **Status**: Active, Maintenance, Deprecated, or Retired
- **Type**: WebApp, API Service, Library, Batch Job, Mobile App, or Other
- **Team**: Search by the owning team name
- **Tags**: Filter by one or more tags
- **Search**: Free-text search across application names and descriptions

## Application Details

Each application page shows:

- **General Information**: Name, description, status, type, and owning team
- **Source Code**: Link to the source code repository (Git or Azure DevOps)
- **Environments**: Links to the application's web environments (Production, Test, UAT, Development)
- **Contacts**: Contact information for the application team (email, Slack, Teams, phone, support URL)
- **Tags**: Labels categorizing the application

## Permissions

| Action | Who Can Do It |
|--------|---------------|
| View applications | All users |
| Create applications | Developers and Admins |
| Edit applications | Application owners (their own), Developers, and Admins |
| Delete applications | Admins only |
| Manage tags | Admins only |

Some environment URLs may be restricted to Developers and Admins if they are marked as non-public.
