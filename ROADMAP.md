# 🗺️ Feed Aggregator – Project Roadmap

A self-hosted, personal feed aggregator built in **ASP.NET Core Razor MVC**. Designed to ingest traditional **RSS feeds**, modern **JSON feeds**, and **webhook-based push sources**, with modular mapping support and a plugin-friendly architecture.

This roadmap is structured in **milestones** — each step produces a usable product with features that progressively expand the system's flexibility and power.

---

## ✅ Milestone 1: Core MVP – RSS Feed Reader

**Goal**: A functional and simple feed reader UI that ingests standard RSS feeds.

### Features
- [x] Add/remove feed URLs via the UI
- [x] Fetch and parse RSS feeds using `System.ServiceModel.Syndication`
- [x] Store feed metadata and entries in a local DB (SQLite)
- [x] Display title, publish date, and summary/content
- [ ] Manual refresh + background polling via `IHostedService`
- [ ] Read/unread entry tracking
- [x] Responsive UI using Bootstrap 5

---

## 🔄 Milestone 2: JSON Feed Support (Static Mapping)

**Goal**: Add ability to fetch and display feeds provided in JSON format (e.g., JSONFeed.org).

### Features
- [ ] Add support for reading JSON-based feeds with standard schema
- [ ] Store & display JSON entries with same UI structure as RSS
- [ ] Manually configure feed URL and static field mappings
- [ ] Use shared data model to unify parsing logic

---

## 🔍 Milestone 3: Basic Filtering and Search

**Goal**: Make it easier to interact with feed data and triage important entries.

### Features
- [ ] Simple text search over titles/descriptions
- [ ] Stack-specific filters (e.g., only show feeds tagged `network`, `linux`, etc.)
- [ ] Tagging support for feeds and entries (manual or by keyword detection)

---

## 🌐 Milestone 4: JSON API Feed Support (Polling)

**Goal**: Support data feeds delivered via custom JSON APIs (REST endpoints, etc.)

### Features
- [ ] Poll external APIs with headers or query strings
- [ ] Treat JSON results like feeds (title/date/content/etc.)
- [ ] Configurable polling interval per feed
- [ ] Store raw responses for debugging

---

## 🔧 Milestone 5: Modular JSON Mapping (Schema Mapper)

**Goal**: Add flexible support for arbitrary JSON feeds using schema mappings

### Features
- [ ] Allow defining field mappings at feed creation time

```json
{
  "title": "article.title",
  "date": "meta.published_at",
  "description": "body",
  "link": "links.read_more"
}
```

- [ ] Store and apply mappings to normalize data
- [ ] Support JSONPath-style resolution (if feasible)

---

## 🔔 Milestone 6: Webhook Notification Support

**Goal**: Accept inbound event notifications from CI/CD systems or custom services

### Features
- [ ] Create webhook receiver endpoints (`/webhook/github`, etc.)
- [ ] Parse payloads and inject them as feed entries
- [ ] Support optional mapping like JSON feeds
- [ ] Filter or transform payloads for known providers (GitHub, GitLab, etc.)

---

## 🧩 Milestone 7: Plugin Architecture

**Goal**: Allow developers to add new sources, filters, mappers, or even UI modules

### Features
- [ ] Define interfaces for `IFeedSource`, `IContentMapper`, etc.
- [ ] Load plugin DLLs from `/plugins` folder using reflection
- [ ] Provide documentation for community contributions
- [ ] Basic plugin discovery UI (list installed plugins)

---

## 🌓 Milestone 8: UI & UX Enhancements

**Goal**: Improve daily usability and engagement with the app

### Features
- [ ] Feed grouping and tagging
- [ ] Custom sorting (newest, unread, per tag)
- [ ] Configurable feed refresh schedule (per-feed basis)
- [ ] Entry pinning (mark important items)

---

## ⚙️ Milestone 9: Performance and Infrastructure

**Goal**: Optimize background tasks and make the app deployment-friendly

### Features
- [ ] ETag / Last-Modified support for HTTP caching
- [ ] Retry queues for failed fetches
- [ ] Environment-based config (for self-hosted or cloud deployment)
- [ ] Dockerfile and `appsettings.json` scaffolding

---

## 🧪 Milestone 10: Testing, DevX, and Docs

**Goal**: Improve maintainability, testability, and onboarding

### Features
- [ ] Unit tests for feed parsers, mappers, and scheduler
- [ ] Integration tests for full fetch → display loop
- [ ] Developer quick-start and contribution guide
- [ ] Predefined JSON/RSS test feeds for testing

---

## 🛣️ Future Ideas (Not yet scheduled)

- [ ] OPML import/export for feed subscriptions
- [ ] User accounts and personal configs
- [ ] Mobile-first PWA wrapper
- [ ] CLI sync tool
- [ ] WebSub (PubSubHubbub) support for near real-time RSS updates

---

_You're welcome to fork this roadmap or propose changes. Feedback and contributions are encouraged!_
