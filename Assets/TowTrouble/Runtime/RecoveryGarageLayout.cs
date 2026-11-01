namespace TowTrouble
{
    // Shared reference-canvas dimensions keep the UI and camera framing aligned.
    public static class RecoveryGarageLayout
    {
        public const float CanvasWidth=1600,CategoryWidth=300,PreviewX=356;
        public const float CatalogWidth=646*.8f;
        public const float CatalogX=CanvasWidth-36-CatalogWidth;
        public const float PreviewWidth=CatalogX-24-PreviewX;
        public const float PreviewCenter=(PreviewX+PreviewWidth*.5f)/CanvasWidth;
    }
}
