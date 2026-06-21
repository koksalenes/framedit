# Mediaration

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?style=flat-square&logo=dotnet)
![FFmpeg](https://img.shields.io/badge/FFmpeg-required-007808?style=flat-square&logo=ffmpeg)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?style=flat-square&logo=docker)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

A self-hosted media processing toolkit. Upload video, audio, or image files and get back exactly what you need - frames, audio tracks, optimized images, or metadata-stripped files - all processed locally, nothing sent to a third party.

## Features

| Tool | What it does |
|---|---|
| **Video Frame Parser** | Extract frames from a video at a chosen FPS as JPG or PNG |
| **Sound Parser** | Strip audio from a video and export as MP3, AAC, WAV, FLAC, or OGG |
| **Image Optimizer** | Batch-compress images with quality control and optional format conversion |
| **Metadata Cleaner** | Remove EXIF and embedded metadata from images and videos |

## Tech Stack

- **Runtime** - [.NET 10](https://dotnet.microsoft.com/) / ASP.NET Core MVC
- **Media processing** - [FFmpeg](https://ffmpeg.org/) (called via `System.Diagnostics.Process` from C# services)
- **Frontend** - Vanilla JS, no framework
- **Containerization** - Docker + Docker Compose

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [FFmpeg](https://ffmpeg.org/download.html) installed and available on `PATH`

> If you're using Docker, FFmpeg is installed automatically inside the container - no local install needed.

## Getting Started

### Run locally

```bash
# Clone the repo
git clone <repo-url>
cd framedit

# Start the app
dotnet run
```

The app will be available at `http://localhost:5050` (or the port shown in your terminal).

### Run with Docker

```bash
docker compose up --build
```

App runs at `http://localhost:5050`.

Temporary files are written to `/tmp/mediaration` inside the container and mounted from the host via the volume in `docker-compose.yml`.

## Upload Limits

| Limit | Value |
|---|---|
| Max total upload size | 1 GB |
| Max files per request | 50 |

## Project Structure

```
Controllers/          # MVC controllers, one per feature
Services/
  VideoFrameParser/   # Frame extraction logic (FFmpeg)
  SoundParser/        # Audio extraction logic (FFmpeg)
  ImageOptimizer/     # Image compression and conversion
  MetadataCleaner/    # EXIF/metadata stripping
Constants/
  AppConstants.cs     # Allowed formats, upload limits, file types
wwwroot/js/           # Client-side JS (one file per feature)
Views/                # Razor views
```

## Supported Formats

**Video input** (Frame Parser, Sound Parser, Metadata Cleaner): `mp4 mov avi mkv webm flv wmv m4v mpeg mpg`

**Image input** (Image Optimizer, Metadata Cleaner): `jpg jpeg png gif bmp tiff tif webp`

**Audio output** (Sound Parser): `mp3 aac wav flac ogg`

**Image output** (Image Optimizer): `jpg png webp` or keep original format
