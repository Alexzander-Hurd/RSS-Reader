[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![project_license][license-shield]][license-url]

# RSS-Reader

A self-hosted, personal feed aggregator built in **ASP.NET Core Razor MVC**. Designed to ingest traditional **RSS feeds**, modern **JSON feeds**, and **webhook-based push sources**, with modular mapping support and a plugin-friendly architecture.

[View on GitHub](https://github.com/Alexzander-Hurd/RSS-Reader)

---

## 📜 Table of Contents

- [About The Project](#about-the-project)
- [Getting Started](#getting-started)
  - [Installation](#installation)
  - [Usage](#usage)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
- [Acknowledgements](#acknowledgements)
- [Security Policy](#security-policy)

---

## 🧠 About The Project

This project is a flexible and extensible tool for aggregating and reading content from a variety of sources:

- Traditional RSS (XML-based)
- JSON Feeds (modern feed standard)
- JSON APIs (with schema mapping)
- Webhook event payloads (GitHub, CI/CD, etc.)

It is modular by design and supports custom plugin development for alternative feed types, transformations, and UI integrations.

---

## 🛠 Built With

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-6E4C7F?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core)
[![Bootstrap 5](https://img.shields.io/badge/Bootstrap-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/index.html)

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/)
- [Docker](https://www.docker.com/) (optional, for container deployment)

### Installation

Clone the repository:

```bash
git clone https://github.com/Alexzander-Hurd/RSS-Reader.git
cd RSS-Reader
```

Build and run locally:

```bash
dotnet build
dotnet run
```

Or build the Docker image:

```bash
docker build -t rss-reader .
docker run -p 5000:80 rss-reader
```

> 📦 A Docker image will be published on GCR and made available as a `.tar.gz` build artifact in future releases.

---

## 💡 Usage

Once deployed:

1. Navigate to the home page.
2. Add RSS or JSON feeds via the UI.
3. Configure polling frequency, schema mappings, and tags.
4. View filtered content via tags or stacks.
5. Extend via plugin modules or webhook receivers.

More detailed examples and screenshots will be added soon.

---

## 🛣 Roadmap

See [ROADMAP.md](ROADMAP.md) for the full development plan.

Planned key features include:

- JSONPath-style mapping for arbitrary API feeds
- Plugin-based extension system
- Webhook-based input
- CI/CD integration support
- Stack/tag filtering and custom views

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Contributions will be governed by a future [`CONTRIBUTING.md`](CONTRIBUTING.md).

---

## 🛡 License

Distributed under the MIT License. See [`LICENSE`](LICENSE) for more information.

© Alexzander Hurd

---

## 📬 Contact

- GitHub: [@Alexzander-Hurd](https://github.com/Alexzander-Hurd)
- Website: [alexhurd.uk](https://www.alexhurd.uk)
- Links: [alexhurd.uk/links](https://www.alexhurd.uk/links)

---

## 🛡 Security Policy

Security disclosures and vulnerability reports will be handled via a formal [`SECURITY.md`](SECURITY.md) in the near future. For now, please contact through GitHub issues or [alexhurd.uk](https://www.alexhurd.uk).

---

## 🙌 Acknowledgements

- [Best README Template](https://github.com/othneildrew/Best-README-Template)
- [RSS 2.0 Specification](https://www.rssboard.org/rss-specification)
- [JSONFeed Spec](https://jsonfeed.org/version/1)
- [.NET](https://dotnet.microsoft.com/)
- [Bootstrap](https://getbootstrap.com/)


[contributors-shield]: https://img.shields.io/github/contributors/Alexzander-Hurd/RSS-Reader.svg?style=for-the-badge
[contributors-url]: https://github.com/Alexzander-Hurd/RSS-Reader/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Alexzander-Hurd/RSS-Reader.svg?style=for-the-badge
[forks-url]: https://github.com/Alexzander-Hurd/RSS-Reader/network/members
[stars-shield]: https://img.shields.io/github/stars/Alexzander-Hurd/RSS-Reader.svg?style=for-the-badge
[stars-url]: https://github.com/Alexzander-Hurd/RSS-Reader/stargazers
[issues-shield]: https://img.shields.io/github/issues/Alexzander-Hurd/RSS-Reader.svg?style=for-the-badge
[issues-url]: https://github.com/Alexzander-Hurd/RSS-Reader/issues
[license-shield]: https://img.shields.io/github/license/Alexzander-Hurd/RSS-Reader.svg?style=for-the-badge
[license-url]: https://github.com/Alexzander-Hurd/RSS-Reader/blob/master/LICENSE.txt