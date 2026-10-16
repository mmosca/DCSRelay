param(
	[string]$DetailedSvg = "AppIcon.svg",
	[string]$SimplifiedSvg = "AppIcon.Simplified.svg",
	[string]$OutputIco = "AppIcon.ico"
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName PresentationCore

$assetsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$detailedPath = Join-Path $assetsDir $DetailedSvg
$simplifiedPath = Join-Path $assetsDir $SimplifiedSvg
$outputPath = Join-Path $assetsDir $OutputIco

if (-not (Test-Path $detailedPath)) {
	throw "Detailed SVG not found: $detailedPath"
}

if (-not (Test-Path $simplifiedPath)) {
	throw "Simplified SVG not found: $simplifiedPath"
}

$culture = [System.Globalization.CultureInfo]::InvariantCulture

function Parse-Double([string]$value) {
	return [double]::Parse($value, $culture)
}

function Parse-Color([string]$value) {
	return [System.Windows.Media.ColorConverter]::ConvertFromString($value)
}

function Parse-LineCap([string]$value) {
	switch ($value) {
		'round' { return [System.Windows.Media.PenLineCap]::Round }
		'square' { return [System.Windows.Media.PenLineCap]::Square }
		default { return [System.Windows.Media.PenLineCap]::Flat }
	}
}

function Parse-LineJoin([string]$value) {
	switch ($value) {
		'bevel' { return [System.Windows.Media.PenLineJoin]::Bevel }
		'miter' { return [System.Windows.Media.PenLineJoin]::Miter }
		default { return [System.Windows.Media.PenLineJoin]::Round }
	}
}

function New-PenFromState($state) {
	if ([string]::IsNullOrWhiteSpace($state.Stroke) -or $state.Stroke -eq 'none') {
		return $null
	}

	$brush = New-Object System.Windows.Media.SolidColorBrush((Parse-Color $state.Stroke))
	$brush.Freeze()

	$pen = New-Object System.Windows.Media.Pen($brush, $state.StrokeWidth)
	$pen.StartLineCap = Parse-LineCap $state.LineCap
	$pen.EndLineCap = Parse-LineCap $state.LineCap
	$pen.LineJoin = Parse-LineJoin $state.LineJoin
	$pen.Freeze()
	return $pen
}

function Merge-State($state, $node) {
	$merged = [ordered]@{
		Stroke = $state.Stroke
		StrokeWidth = $state.StrokeWidth
		LineCap = $state.LineCap
		LineJoin = $state.LineJoin
	}

	if ($node.Attributes['stroke']) {
		$merged.Stroke = $node.Attributes['stroke'].Value
	}

	if ($node.Attributes['stroke-width']) {
		$merged.StrokeWidth = Parse-Double $node.Attributes['stroke-width'].Value
	}

	if ($node.Attributes['stroke-linecap']) {
		$merged.LineCap = $node.Attributes['stroke-linecap'].Value
	}

	if ($node.Attributes['stroke-linejoin']) {
		$merged.LineJoin = $node.Attributes['stroke-linejoin'].Value
	}

	return $merged
}

function Draw-SvgNode($dc, $node, $state) {
	if ($node.NodeType -ne [System.Xml.XmlNodeType]::Element) {
		return
	}

	$local = $node.LocalName
	$effective = Merge-State $state $node

	switch ($local) {
		'svg' {
			foreach ($child in $node.ChildNodes) {
				Draw-SvgNode -dc $dc -node $child -state $effective
			}
		}

		'g' {
			foreach ($child in $node.ChildNodes) {
				Draw-SvgNode -dc $dc -node $child -state $effective
			}
		}

		'circle' {
			$pen = New-PenFromState $effective
			if ($null -eq $pen) { return }

			$cx = Parse-Double $node.Attributes['cx'].Value
			$cy = Parse-Double $node.Attributes['cy'].Value
			$r = Parse-Double $node.Attributes['r'].Value

			$dc.DrawEllipse($null, $pen, (New-Object System.Windows.Point($cx, $cy)), $r, $r)
		}

		'line' {
			$pen = New-PenFromState $effective
			if ($null -eq $pen) { return }

			$x1 = Parse-Double $node.Attributes['x1'].Value
			$y1 = Parse-Double $node.Attributes['y1'].Value
			$x2 = Parse-Double $node.Attributes['x2'].Value
			$y2 = Parse-Double $node.Attributes['y2'].Value

			$dc.DrawLine($pen, (New-Object System.Windows.Point($x1, $y1)), (New-Object System.Windows.Point($x2, $y2)))
		}

		'path' {
			$pen = New-PenFromState $effective
			if ($null -eq $pen) { return }

			$d = $node.Attributes['d'].Value
			$geometry = [System.Windows.Media.Geometry]::Parse($d)
			$geometry.Freeze()
			$dc.DrawGeometry($null, $pen, $geometry)
		}

		default {
			throw "Unsupported SVG element '$local' in $($node.OwnerDocument.BaseURI)."
		}
	}
}

function Render-SvgToPngBytes {
	param(
		[string]$SvgPath,
		[int]$Size
	)

	[xml]$xml = Get-Content -Path $SvgPath -Raw
	$root = $xml.DocumentElement
	if ($null -eq $root -or $root.LocalName -ne 'svg') {
		throw "Invalid SVG root in $SvgPath"
	}

	$viewBox = $root.Attributes['viewBox'].Value -split '\s+'
	if ($viewBox.Length -ne 4) {
		throw "SVG must define a 4-value viewBox: $SvgPath"
	}

	$vbX = Parse-Double $viewBox[0]
	$vbY = Parse-Double $viewBox[1]
	$vbWidth = Parse-Double $viewBox[2]
	$vbHeight = Parse-Double $viewBox[3]

	$scale = [Math]::Min($Size / $vbWidth, $Size / $vbHeight)
	$scaledWidth = $vbWidth * $scale
	$scaledHeight = $vbHeight * $scale
	$offsetX = ($Size - $scaledWidth) / 2.0
	$offsetY = ($Size - $scaledHeight) / 2.0

	$visual = New-Object System.Windows.Media.DrawingVisual
	$dc = $visual.RenderOpen()

	$dc.PushTransform((New-Object System.Windows.Media.TranslateTransform($offsetX, $offsetY)))
	$dc.PushTransform((New-Object System.Windows.Media.ScaleTransform($scale, $scale)))
	$dc.PushTransform((New-Object System.Windows.Media.TranslateTransform(-$vbX, -$vbY)))

	$initialState = [ordered]@{
		Stroke = $null
		StrokeWidth = 1.0
		LineCap = 'butt'
		LineJoin = 'round'
	}

	Draw-SvgNode -dc $dc -node $root -state $initialState

	$dc.Close()

	$bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($Size, $Size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
	$bitmap.Render($visual)

	$encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
	$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))

	$ms = New-Object System.IO.MemoryStream
	$encoder.Save($ms)
	$bytes = $ms.ToArray()
	$ms.Dispose()
	return $bytes
}

$frames = @(
	@{ Size = 256; Png = (Render-SvgToPngBytes -SvgPath $detailedPath -Size 256) },
	@{ Size = 128; Png = (Render-SvgToPngBytes -SvgPath $detailedPath -Size 128) },
	@{ Size = 64; Png = (Render-SvgToPngBytes -SvgPath $simplifiedPath -Size 64) },
	@{ Size = 32; Png = (Render-SvgToPngBytes -SvgPath $simplifiedPath -Size 32) },
	@{ Size = 16; Png = (Render-SvgToPngBytes -SvgPath $simplifiedPath -Size 16) }
)

$fs = [System.IO.File]::Open($outputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([int16]0)
$bw.Write([int16]1)
$bw.Write([int16]$frames.Count)

$offset = 6 + (16 * $frames.Count)
foreach ($frame in $frames) {
	$dim = if ($frame.Size -ge 256) { 0 } else { $frame.Size }
	$bw.Write([byte]$dim)
	$bw.Write([byte]$dim)
	$bw.Write([byte]0)
	$bw.Write([byte]0)
	$bw.Write([int16]1)
	$bw.Write([int16]32)
	$bw.Write([int32]$frame.Png.Length)
	$bw.Write([int32]$offset)
	$offset += $frame.Png.Length
}

foreach ($frame in $frames) {
	$bw.Write([byte[]]$frame.Png)
}

$bw.Flush()
$bw.Dispose()
$fs.Dispose()

Write-Host "Created: $outputPath"
