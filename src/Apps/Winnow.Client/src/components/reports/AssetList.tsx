import { Paperclip, Loader2, ShieldCheck, ShieldAlert, AlertCircle } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { MediaGallery } from "@/components/MediaGallery";
import { cn } from "@/lib/utils";

interface AssetData {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: string; // Pending, Clean, Infected, Failed
  downloadUrl?: string;
}

interface AssetListProps {
  assets: AssetData[];
  isLocked?: boolean;
}

export function AssetList({ assets, isLocked }: AssetListProps) {
  if (!assets || assets.length === 0) return null;

  return (
    <Card className="rounded-3xl border-white/10 shadow-2xl overflow-hidden">
      <CardHeader className="bg-muted/30">
        <CardTitle className="text-sm font-bold flex items-center gap-2">
          <Paperclip className="h-4 w-4 text-blue-500" />
          Attachments
          <Badge variant="neutral" className="ml-auto text-xs h-5">
            {assets.length} file{assets.length > 1 ? "s" : ""}
          </Badge>
        </CardTitle>
        <CardDescription className="text-xs">
          Files captured with this report.
        </CardDescription>
      </CardHeader>
      <CardContent
        className={cn(
          "space-y-4 pt-6",
          isLocked ? "blur-md select-none pointer-events-none opacity-60" : ""
        )}
      >
        {/* Show clean images in MediaGallery */}
        {assets.filter(
          (a) =>
            a.status === "Clean" &&
            a.downloadUrl &&
            (a.contentType.startsWith("image/") ||
              a.contentType.startsWith("video/")),
        ).length > 0 && (
          <MediaGallery
            attachments={assets
              .filter(
                (a) =>
                  a.status === "Clean" &&
                  a.downloadUrl &&
                  (a.contentType.startsWith("image/") ||
                    a.contentType.startsWith("video/")),
              )
              .map((a) => ({
                url: a.downloadUrl!,
                type: a.contentType,
                filename: a.fileName,
              }))}
          />
        )}

        {/* Status list for all assets */}
        <div className="space-y-2">
          {assets.map((asset) => (
            <div
              key={asset.id}
              className="flex items-center gap-3 text-xs rounded-2xl border border-white/5 bg-muted/20 p-3 transition-colors hover:bg-muted/40"
            >
              {asset.status === "Pending" && (
                <Badge
                  variant="outline"
                  className="gap-1.5 text-amber-600 border-amber-500/20 bg-amber-500/5 text-[10px] px-2"
                >
                  <Loader2 className="h-3 w-3 animate-spin" />
                  Scanning
                </Badge>
              )}
              {asset.status === "Clean" && (
                <Badge
                  variant="outline"
                  className="gap-1.5 text-emerald-600 border-emerald-500/20 bg-emerald-500/5 text-[10px] px-2"
                >
                  <ShieldCheck className="h-3 w-3" />
                  Clean
                </Badge>
              )}
              {asset.status === "Infected" && (
                <Badge
                  variant="destructive"
                  className="gap-1.5 text-[10px] px-2"
                >
                  <ShieldAlert className="h-3 w-3" />
                  Infected
                </Badge>
              )}
              {asset.status === "Failed" && (
                <Badge
                  variant="outline"
                  className="gap-1.5 text-red-600 border-red-500/20 bg-red-500/5 text-[10px] px-2"
                >
                  <AlertCircle className="h-3 w-3" />
                  Failed
                </Badge>
              )}
              <span className="truncate flex-1 font-medium">
                {asset.fileName}
              </span>
              <span className="text-muted-foreground text-[10px] tabular-nums">
                {(asset.sizeBytes / 1024).toFixed(1)} KB
              </span>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
