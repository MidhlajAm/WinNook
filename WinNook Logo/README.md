# WinNook icon system

## The mark

Four panes with the island cutting across them. The panes are the desktop; the dark band is
WinNook sitting over it. The bottom-right pane is deliberately narrow and light — it breaks the
2x2 grid so the mark does not read as the Windows logo, and it gives the composition an accent
that survives at medium sizes.

## Palette

| Token | Hex | Use |
|---|---|---|
| Accent | `#0078D4` | Panes, small-size tile |
| Highlight | `#4CC2FF` | Narrow pane, status dot |
| Pane (small variant) | `#FFFFFF` | Panes when on the blue tile |
| Pane accent (small) | `#9EDBFF` | Narrow pane on the blue tile |
| Island | `#16181D` | The band. Never pure black. |

Geometry is drawn on a 256 grid. Pane corner radius is 16; the island radius is 24 (half its
height, so it is a true pill). Keep that relationship if you rescale anything — a pane that is
more rounded than the island makes the island stop reading as a pill.

## Files

```
svg/winnook-icon.svg         master, use at 40px and above
svg/winnook-icon-small.svg   simplified, use at 32px and below
svg/winnook-icon-mono.svg    currentColor, for tray / monochrome contexts
winnook.ico                  16,20,24,32,40,48,64,128,256 packed
png/winnook-<size>.png       individual raster sizes
png/winnook-mono-<color>-<size>.png  tray renders, white and black
```

Two variants exist because the island is dark. On a default Windows 11 taskbar the free-form
master loses the band into the background at small sizes. The small variant puts everything on a
blue tile so the island always has something to contrast against. This is the same reason macOS
and Windows system icons ship separate small artwork — it is not optional polish.

## Using it

**App / exe icon.** Point your build at `winnook.ico`. In a .NET or WinUI project that is
`<ApplicationIcon>winnook.ico</ApplicationIcon>` in the .csproj. For Inno Setup or NSIS
installers, the same file works for both the installer and the shortcut.

**MSIX / Store packaging.** If you package it, Windows wants a named set: Square44x44Logo,
Square150x150Logo, Wide310x150Logo, StoreLogo, and their scale variants (100/125/150/200/400).
Say the word and I will generate that folder with the exact filenames.

**System tray.** Use `png/winnook-mono-white-16.png` and `-black-16.png`, and switch on the
system light/dark setting. A colored tray icon in Windows 11 reads as clutter next to the
built-in glyphs.

**Website / GitHub.** `svg/winnook-icon.svg` directly. For a favicon, the 32 and 16 PNGs plus the
ico cover every browser.

## Things to keep consistent

- Do not put the master variant on a colored background. It is designed to sit on the desktop
  wallpaper or a neutral surface.
- Do not recolor the island to blue. The dark band is the product; making it blue removes the
  only thing that distinguishes the mark from a generic grid.
- Leave clear space of at least one pane-width around the mark in marketing use.
