# StreamMediaSource Implementation

## Overview

This document summarizes the implementation of `StreamMediaSource`, a new media source type that allows passing a .NET `Stream` directly to `MediaElement` for playback.

## What Was Added

### Core Class
- **`StreamMediaSource.shared.cs`**: New `MediaSource` subclass that wraps a .NET `Stream`
  - Includes `Stream` bindable property
  - Implicit operators for convenient Stream conversion
  - Factory method `MediaSource.FromStream(Stream)`
  - Follows the same pattern as `FileMediaSource`, `UriMediaSource`, and `ResourceMediaSource`

### Platform-Specific Implementations

#### Windows (`MediaManager.windows.cs`)
- **Implementation**: Uses `WinMediaSource.CreateFromStream()` with `IRandomAccessStream`
- **Status**: ✅ Fully Supported
- **Method**: Leverages `WindowsRuntimeStreamExtensions.AsRandomAccessStream()` to convert .NET streams to Windows Runtime streams
- **Helper**: `StreamExtensions.windows.cs` - Provides `GetMimeType()` extension method

#### Android (`MediaManager.android.cs`)
- **Implementation**: Custom `StreamDataSource` implementing ExoPlayer's `IDataSource` interface
- **Status**: ✅ Fully Supported
- **Method**: Created custom data source that reads from .NET Stream and integrates with ExoPlayer's `ProgressiveMediaSource`
- **Helper**: `StreamDataSource.android.cs` - Contains `StreamDataSource` and `StreamDataSourceFactory` classes
- **Features**:
  - Supports seeking if stream is seekable
  - Handles range requests
  - Proper resource cleanup and disposal

#### iOS/macOS (`MediaManager.macios.cs`)
- **Implementation**: Uses `AVAssetResourceLoaderDelegate` to provide custom data source
- **Status**: ✅ Fully Supported
- **Method**: Creates custom URL scheme (`stream://media`) and uses `AVAssetResourceLoader` to intercept loading requests
- **Helper**: `StreamAssetResourceLoader.macios.cs` - Custom delegate that feeds stream data to `AVPlayer`
- **Features**:
  - Works with both audio and video
  - Supports seeking if stream is seekable
  - Integrates seamlessly with existing `AVPlayer` architecture

#### Tizen (`MediaManager.tizen.cs`)
- **Status**: ❌ Not Supported
- **Reason**: Platform limitation - throws `NotSupportedException` with helpful message

## Usage Examples

```csharp
// Using factory method
MediaSource source = MediaSource.FromStream(myStream);

// Using implicit conversion
MediaSource source = myStream;

// Direct instantiation
var streamSource = new StreamMediaSource { Stream = myStream };

// Assigning to MediaElement
mediaElement.Source = MediaSource.FromStream(myStream);
```

## Files Modified

### New Files
1. `MediaSource/StreamMediaSource.shared.cs` - Core class
2. `Platforms/Android/StreamDataSource.android.cs` - Android implementation
3. `Views/StreamAssetResourceLoader.macios.cs` - iOS/macOS implementation
4. `Views/StreamExtensions.windows.cs` - Windows helper extensions

### Modified Files
1. `MediaSource/MediaSource.shared.cs` - Added `FromStream()` factory method
2. `Views/MediaManager.android.cs` - Added stream support via custom DataSource
3. `Views/MediaManager.windows.cs` - Added stream support via `CreateFromStream()`
4. `Views/MediaManager.macios.cs` - Added stream support via `AVAssetResourceLoader`
5. `Views/MediaManager.tizen.cs` - Added NotSupportedException for streams
6. `Converters/MediaSourceConverter.shared.cs` - Added `StreamMediaSource` case

## Technical Details

### Android Implementation
The Android implementation uses ExoPlayer's extensibility model:
- `StreamDataSource` implements `IDataSource` interface
- Reads data from .NET Stream on demand
- `StreamDataSourceFactory` creates instances for ExoPlayer
- Integrates with `ProgressiveMediaSource.Factory`

### iOS/macOS Implementation
The iOS/macOS implementation uses Apple's official extension mechanism:
- Creates `AVUrlAsset` with custom URL scheme
- `AVAssetResourceLoaderDelegate` intercepts resource loading requests
- Provides stream data through `AVAssetResourceLoadingDataRequest`
- Supports both content information and data requests

### Windows Implementation
The Windows implementation uses built-in .NET stream conversion:
- Converts .NET Stream to `IRandomAccessStream` using `AsRandomAccessStream()`
- Uses `WinMediaSource.CreateFromStream()` API
- Provides default MIME type (can be enhanced for better type detection)

## Considerations

### Stream Lifecycle
- Streams are **not** automatically disposed by the MediaElement
- Callers are responsible for managing stream lifetime
- Consider keeping streams open for the duration of playback
- Seeking requires seekable streams

### MIME Type Detection
- Windows: Returns generic `"application/octet-stream"` by default
- iOS/macOS: Defaults to `"video/mp4"` assumption
- Future enhancement: Could detect MIME type from stream content (magic bytes)

### Performance
- Streams should support efficient random access for best performance
- Non-seekable streams may have limited functionality
- Consider buffering strategies for network streams

## Future Enhancements

1. **MIME Type Detection**: Implement magic byte detection to automatically determine content type
2. **Tizen Support**: Investigate if Tizen platform can support stream-based playback
3. **Stream Properties**: Add optional properties to `StreamMediaSource` for MIME type hint
4. **Buffering Options**: Add configurable buffering strategies for different stream types
5. **Progress Reporting**: Add stream read progress callbacks for monitoring
