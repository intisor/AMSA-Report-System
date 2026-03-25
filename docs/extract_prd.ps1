Add-Type -AssemblyName System.IO.Compression.FileSystem
$docxPath = 'C:\Users\DELL\Desktop\Coded\AMSA\AMSAReportingSystem\docs\AMSA_Reporting_System_PRD.docx'
$tempDir = [System.IO.Path]::GetTempPath() + 'docx_' + [System.Guid]::NewGuid()
[System.IO.Compression.ZipFile]::ExtractToDirectory($docxPath, $tempDir)
$xml = [xml](Get-Content (Join-Path $tempDir 'word\document.xml'))
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace('w', 'http://schemas.openxmlformats.org/wordprocessingml/2006/main')
$textNodes = $xml.SelectNodes('//w:t', $ns)
$text = ($textNodes | ForEach-Object { $_.InnerText }) -join ' '
$text | Out-File -FilePath 'C:\Users\DELL\Desktop\Coded\AMSA\AMSAReportingSystem\docs\PRD_EXTRACTED.txt' -Encoding UTF8
Write-Host 'PRD extracted successfully'
