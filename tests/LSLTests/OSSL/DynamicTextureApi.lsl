default
{
	state_entry()
	{
		string s;
		s = osMovePen(s, 10, 10);
		s = osDrawLine(s, 10, 10, 20, 20);
		s = osDrawLine(s, 30, 30);
		s = osDrawText(s, "Text");
		s = osDrawEllipse(s, 10, 10);
		s = osDrawRectangle(s, 10, 10);
		s = osDrawFilledRectangle(s, 10, 10);
		s = osDrawPolygon(s, [1, 2], [1, 2]);
		s = osDrawFilledPolygon(s, [1, 2], [1, 2]);
		s = osDrawImage(s, 10, 10, "http://example.com/image.png");
		vector v;
		v = osGetDrawStringSize("vector", TextToDraw, "Arial", 14);
		s = osSetFontName(s, "Arial");
		s = osSetFontSize(s, 10);
		s = osSetPenSize(s, 10);
		s = osSetPenColor(s, "Black");
		s = osSetPenCap(s, "start", "arrow");
		osSetDynamicTextureData("", "vector", s, "width:256,height:256", 0);
		osSetDynamicTextureDataBlend("", "vector", s, "width:256,height:256", 0, 0.5);
		osSetDynamicTextureDataBlendFace("", "vector", s, "width:256,height:256", 0, 0.5, 1);
		osSetDynamicTextureURL("", "image", "http://example.com/image.png", "width:256,height:256", 600);
		osSetDynamicTextureURLBlend("", "image", "http://example.com/image.png", "width:256,height:256", 600, 0.5);
		osSetDynamicTextureURLBlendFace("", "image", "http://example.com/image.png", "width:256,height:256", 600, 0.5, 1);
	}
}