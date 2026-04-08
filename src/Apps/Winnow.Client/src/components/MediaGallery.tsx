import { useState } from "react";
import { Play, X, ZoomIn, Download, FileText, Film } from "lucide-react";

interface Attachment {
  url: string;
  type: string;
  filename: string;
}

interface MediaGalleryProps {
  attachments: Attachment[];
}

export function MediaGallery({ attachments }: MediaGalleryProps) {
  const [lightboxIndex, setLightboxIndex] = useState<number | null>(null);
  const [zoomed, setZoomed] = useState(false);

  if (!attachments.length) return null;

  const isImage = (type: string) => type.startsWith("image/");
  const isVideo = (type: string) => type.startsWith("video/");

  const current = lightboxIndex !== null ? attachments[lightboxIndex] : null;

  return (
    <>
      {/* Thumbnail Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
        {attachments.map((att, i) => (
          <button
            key={i}
            onClick={() => {
              setLightboxIndex(i);
              setZoomed(false);
            }}
            className="group relative aspect-square rounded-xl overflow-hidden border border-border/50 bg-muted/30 hover:border-primary/50 hover:shadow-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:ring-offset-2 focus:ring-offset-background"
          >
            {isImage(att.type) ? (
              <img
                src={att.url}
                alt={att.filename}
                className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
              />
            ) : isVideo(att.type) ? (
              <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-muted to-muted/60">
                <Film className="w-10 h-10 text-muted-foreground/60" />
              </div>
            ) : (
              <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-muted to-muted/60">
                <FileText className="w-10 h-10 text-muted-foreground/60" />
              </div>
            )}

            {/* Video Play Overlay */}
            {isVideo(att.type) && (
              <div className="absolute inset-0 flex items-center justify-center">
                <div className="w-12 h-12 rounded-full bg-background/80 backdrop-blur-sm flex items-center justify-center shadow-lg group-hover:bg-primary group-hover:text-primary-foreground transition-colors duration-200">
                  <Play className="w-5 h-5 ml-0.5" />
                </div>
              </div>
            )}

            {/* Zoom overlay for images */}
            {isImage(att.type) && (
              <div className="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-200 bg-black/20">
                <ZoomIn className="w-6 h-6 text-white drop-shadow-lg" />
              </div>
            )}

            {/* Filename label */}
            <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-black/60 to-transparent px-2 py-1.5">
              <span className="text-xs text-white truncate block font-medium">
                {att.filename}
              </span>
            </div>
          </button>
        ))}
      </div>

      {/* Lightbox Modal */}
      {current && lightboxIndex !== null && (
        <div
          role="button"
          tabIndex={-1}
          className="fixed inset-0 z-[9999] bg-black/90 backdrop-blur-sm flex flex-col items-center justify-center animate-in fade-in duration-200"
          onClick={() => {
            setLightboxIndex(null);
            setZoomed(false);
          }}
          onKeyDown={(e) => {
            if (e.key === "Escape") {
              setLightboxIndex(null);
              setZoomed(false);
            }
          }}
        >
          {/* Close button */}
          <button
            onClick={() => {
              setLightboxIndex(null);
              setZoomed(false);
            }}
            className="absolute top-4 right-4 z-10 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center transition-colors"
          >
            <X className="w-5 h-5 text-white" />
          </button>

          {/* Download button */}
          <a
            href={current.url}
            download={current.filename}
            onClick={(e) => e.stopPropagation()}
            className="absolute top-4 right-16 z-10 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center transition-colors"
            title="Download"
          >
            <Download className="w-5 h-5 text-white" />
          </a>

          {/* Navigation */}
          {attachments.length > 1 && (
            <>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setLightboxIndex(
                    (lightboxIndex - 1 + attachments.length) %
                      attachments.length,
                  );
                  setZoomed(false);
                }}
                className="absolute left-4 top-1/2 -translate-y-1/2 z-10 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center transition-colors text-white text-xl font-light"
              >
                ‹
              </button>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setLightboxIndex((lightboxIndex + 1) % attachments.length);
                  setZoomed(false);
                }}
                className="absolute right-4 top-1/2 -translate-y-1/2 z-10 w-10 h-10 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center transition-colors text-white text-xl font-light"
              >
                ›
              </button>
            </>
          )}

          {/* Content */}
          <div
            className="max-w-[90vw] max-h-[85vh] flex items-center justify-center"
          >
            {isImage(current.type) ? (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setZoomed(!zoomed);
                }}
                className={`flex items-center justify-center border-none bg-transparent p-0 transition-transform duration-300 ${
                  zoomed ? "scale-150 cursor-zoom-out" : "cursor-zoom-in"
                }`}
                aria-label={zoomed ? "Zoom out" : "Zoom in"}
              >
                <img
                  src={current.url}
                  alt={current.filename}
                  className="max-h-[85vh] max-w-[90vw] object-contain rounded-lg shadow-2xl"
                />
              </button>
            ) : isVideo(current.type) ? (
              <div
                className="w-[80vw] max-w-4xl aspect-video rounded-lg overflow-hidden bg-black shadow-2xl"
                onClick={(e) => e.stopPropagation()}
                role="presentation"
              >
                <video
                  src={current.url}
                  controls
                  autoPlay
                  className="w-full h-full"
                  style={{
                    colorScheme: "dark",
                  }}
                >
                  <track kind="captions" />
                  Your browser does not support the video tag.
                </video>
              </div>
            ) : (
              <div className="bg-card rounded-lg p-8 max-w-md text-center shadow-2xl">
                <FileText className="w-16 h-16 text-muted-foreground mx-auto mb-4" />
                <p className="text-foreground font-medium mb-2">
                  {current.filename}
                </p>
                <a
                  href={current.url}
                  download={current.filename}
                  className="text-sm text-primary hover:underline"
                >
                  Download File
                </a>
              </div>
            )}
          </div>

          {/* Filename bar */}
          <div className="absolute bottom-4 text-center">
            <span className="text-sm text-white/70 bg-black/50 px-3 py-1.5 rounded-full">
              {current.filename}
              {attachments.length > 1 && (
                <span className="ml-2 text-white/50">
                  {lightboxIndex + 1} / {attachments.length}
                </span>
              )}
            </span>
          </div>
        </div>
      )}
    </>
  );
}
