#include <gtk/gtk.h>

void ui_glue_set_drag_pixbuf_from_image (GdkDragContext *drag_context,
					 GtkImage *image);


void
ui_glue_set_drag_pixbuf_from_image (GdkDragContext *drag_context,
				    GtkImage *image)
{
	if (image->storage_type == GTK_IMAGE_PIXBUF &&
	    image->data.pixbuf.pixbuf) {
		gtk_drag_set_icon_pixbuf (drag_context,
					  image->data.pixbuf.pixbuf,
					  0, 0);
	} else if (image->storage_type == GTK_IMAGE_ICON_NAME &&
		   image->data.name.pixbuf) {
		gtk_drag_set_icon_pixbuf (drag_context,
					  image->data.name.pixbuf,
					  0, 0);
	}
}
