# QPeek

## Project Idea

This is a small Windows desktop utility inspired by **macOS Quick Look** and the existing Windows application **QuickLook**.

The basic interaction is simple:

> Select a file in Windows Explorer → press `Space` → instantly preview it without opening the full application.

This project is currently primarily a **learning and experimentation project**, not a product that needs to become commercially successful.

The goal is to build a small but complete Windows application, learn how Windows desktop utilities work, and gradually improve the product through actual use.

---

## Current Goal: SLC

For the first version, follow the **SLC — Simple, Lovable, Complete** approach.

The goal is **not** to implement every feature found in QuickLook or macOS Quick Look.

Instead, build the smallest version that already feels like a real, usable application.

### Core interaction

1. The user selects a file in Windows Explorer.
2. The user presses `Space`.
3. A lightweight preview window appears.
4. Pressing `Space` or `Esc` closes the preview.

The preview should feel fast and lightweight.

### Initial file support

For the first SLC, focus only on a very small number of formats.

Priority:

- Images
  - JPG / JPEG
  - PNG
  - WEBP

- Text
  - TXT
  - Markdown (`.md`)

Do **not** try to support every possible file format in the first version.

Video, PDF, Office documents, RAW images, PSD, archives, etc. can wait.

### Preview navigation

When the preview window is open:

- `←` / `→` can switch between adjacent files in the current folder.
- Switching between different supported file types should work inside the same preview experience.

For example:

`image.jpg → notes.md → another-image.png`

### Open with default application

The preview window should provide a simple way to open the current file using its system default application.

This may eventually be:

- an `Open` button;
- an icon in the preview window;
- or a keyboard shortcut.

For the SLC, choose whichever implementation is simplest and reliable.

---

## Product Principles

For now, prioritize these qualities:

### 1. Fast

The preview should appear as quickly as reasonably possible after pressing `Space`.

### 2. Simple

Avoid settings, menus, plugins, complicated toolbars, and unnecessary controls in the first version.

### 3. Native-feeling

The application should feel like a small extension of Windows rather than a large standalone program.

### 4. Do not over-engineer

This is an experimental first project.

Prefer a straightforward implementation that works over a highly abstract or excessively extensible architecture.

Do not build infrastructure for hypothetical future requirements unless the current SLC actually needs it.

---

## Not in the Current SLC

The following are explicitly **out of scope for now**:

- Video playback
- GIF playback
- PDF preview
- Office documents
- PSD / AI / RAW
- Archive preview
- Plugin system
- File management
- Editing
- Cloud features
- AI features
- Complex settings
- Hover-to-preview
- Commercial licensing / account system

Do not implement these unless the project scope is explicitly changed later.

---

## Possible Future Direction

If the SLC works well, the project may gradually explore:

### More media formats

Possible next steps:

`GIF → PDF → Video`

Video preview could eventually become an important part of the application, especially for quickly browsing media files.

### Better visual experience

The preview window could gradually become more polished:

- smoother opening / closing animation;
- better Windows 11 visual integration;
- clean borderless interface;
- image metadata;
- responsive preview sizing;
- dark / light mode.

### Faster media browsing

The application may eventually focus more strongly on quickly browsing images and videos.

Potential users could include people who regularly manage lots of media files, but this is **not currently a strict product positioning requirement**.

### More file formats

Later versions may experiment with:

- SVG
- PDF
- video
- audio
- code
- RAW
- PSD
- Office documents

However, supporting many formats is not currently the main goal.

---

## Development Philosophy

This project should grow **one small complete version at a time**.

Do not attempt to reproduce all of QuickLook.

The current question is simply:

> Can we build a small Windows utility where selecting a file and pressing `Space` gives the user a fast, pleasant preview?

If that works, we can decide what the next version should become based on what we learn while building and using it.
