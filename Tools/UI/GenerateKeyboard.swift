import AppKit
let output=CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : "Assets/TowTrouble/Art/UI/InputKeyboard"
var keys=[String:String]()
for c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" {keys[String(c)]=String(c)}
for i in 1...12 {keys["F\(i)"]="F\(i)"}
for (id,label) in ["Enter":"↵","Esc":"esc","Tab":"⇥","Space":"space","Del":"del","Up":"↑","Down":"↓","Left":"←","Right":"→","WASD":"WASD","Arrows":"↑↓←→","LMB":"LMB","RMB":"RMB","Wheel":"↕","Shift":"shift","Ctrl":"ctrl","Alt":"alt","PageUp":"pg↑","PageDown":"pg↓","Backspace":"⌫","Home":"home","End":"end","Insert":"ins","Minus":"−","Equals":"=","Backquote":"`","LeftBracket":"[","RightBracket":"]","Semicolon":";","Quote":"'","Backslash":"\\","Comma":",","Period":".","Slash":"/"] { keys[id]=label }
for (id,label) in keys {
 let wide=["Space","WASD","Arrows","Shift","Ctrl","Backspace","PageDown","PageUp","Home","Insert"].contains(id)
 let w:CGFloat=wide ? 184:128, h:CGFloat=112
 let bitmap=NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:Int(w),pixelsHigh:Int(h),bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:0,bitsPerPixel:0)!
 NSGraphicsContext.saveGraphicsState();NSGraphicsContext.current=NSGraphicsContext(bitmapImageRep:bitmap)
 NSColor.clear.setFill();NSRect(x:0,y:0,width:w,height:h).fill()
 NSColor(calibratedRed:0.045,green:0.055,blue:0.06,alpha:0.95).setFill();NSBezierPath(roundedRect:NSRect(x:4,y:3,width:w-8,height:h-8),xRadius:17,yRadius:17).fill()
 NSColor(calibratedRed:0.19,green:0.22,blue:0.24,alpha:1).setFill();let face=NSBezierPath(roundedRect:NSRect(x:5,y:13,width:w-10,height:h-18),xRadius:14,yRadius:14);face.fill()
 NSColor(calibratedRed:0.43,green:0.47,blue:0.46,alpha:1).setStroke();face.lineWidth=2;face.stroke()
 NSColor(calibratedRed:1,green:0.71,blue:0.23,alpha:0.75).setFill();NSBezierPath(roundedRect:NSRect(x:w*0.34,y:8,width:w*0.32,height:3),xRadius:1.5,yRadius:1.5).fill()
 let size:CGFloat=label.count>3 ? 35 : label.count>1 ? 40:58
 let attrs:[NSAttributedString.Key:Any]=[.font:NSFont.boldSystemFont(ofSize:size),.foregroundColor:NSColor(calibratedRed:1,green:0.96,blue:0.83,alpha:1)]
 let text=label as NSString;let sz=text.size(withAttributes:attrs);text.draw(at:NSPoint(x:(w-sz.width)/2,y:17+(h-30-sz.height)/2),withAttributes:attrs)
 NSGraphicsContext.restoreGraphicsState()
 try bitmap.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:output+"/Keyboard_"+id+".png"))
}
print("Generated \(keys.count) original keyboard glyphs")
