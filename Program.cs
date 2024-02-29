var source = "./locaties";
var target = "./docs";
var qrs = "./qrs";
var tempaltePath = "./template.html";
var wd = "./";

if(!Directory.Exists(source))
{
    Console.WriteLine("bad");
    return;
}

// read root
var paths = Directory.GetFiles(source).OrderBy(x => x).ToArray();

var templateLines = File.ReadAllLines(tempaltePath)!;

var mapBegin = templateLines.Select((line, index) => (index, line)).First(x => x.line.Contains("@@BEGIN_NEXT")).index;
var mapEnd = templateLines.Select((line, index) => (index, line)).First(x => x.line.Contains("@@END_NEXT")).index;

var pre = string.Join("\n", templateLines[..mapBegin]);
var mid = string.Join("\n", templateLines[(mapBegin+1)..mapEnd]);
var post = string.Join("\n", templateLines[(mapEnd+1)..]);

var template = $"{pre}\n{mid}\n{post}";
var templateLast = $"{pre}\n{post}";

// mkdir
Directory.CreateDirectory(target);
Directory.CreateDirectory(qrs);

// assets
var css1 = Path.Combine(wd, "style.css");
var css2 = Path.Combine(target, "style.css");
if(File.Exists(css2))File.Delete(css2);
File.Copy(css1, css2);
var img1 = Path.Combine(wd, "img");
var img2 = Path.Combine(target, "img");
if(Directory.Exists(img2))Directory.Delete(img2, true);
Directory.CreateDirectory(img2);
foreach(var f in Directory.GetFiles(img1))
    File.Copy(f, Path.Combine(img2, Path.GetFileName(f)));

// read locs
var dat = new List<Data>();
foreach(var path in paths)
{
    var fName = Path.GetFileNameWithoutExtension(path);
    var tPath = Path.Combine(target, $"{fName}.html");

    Console.WriteLine($"read {path}");

    var lines = File.ReadAllLines(path);

    var t = lines[0].Trim();
    var p = lines[1].Trim();
    var b = string.Join("\n", lines[2..]).Trim();

    dat.Add(new Data(t, p, b, tPath));
}

// write html
for(var i=0;i<dat.Count;i++)
{
    var last = i == dat.Count - 1;

    var d = dat[i];
    string txt;
    if(last){
        //txt = templateLast.Replace("{{title}}", d.Title).Replace("{{info}}", d.Body);
        var n = dat[0];
        txt = template.Replace("{{title}}", d.Title).Replace("{{info}}", d.Body).Replace("{{next}}", n.Location);
    }
    else{
        var n = dat[i+1];
        txt = template.Replace("{{title}}", d.Title).Replace("{{info}}", d.Body).Replace("{{next}}", n.Location);
    }

    Console.WriteLine($"write {d.Html}");
    File.WriteAllText(d.Html, txt);
}

// get qrs
System.Console.WriteLine("get qrs");
var client = new HttpClient();
foreach(var path in paths)
{
    var fName = Path.GetFileNameWithoutExtension(path);
    var png = Path.Combine(qrs, $"{fName}.png");

    if(File.Exists(png)) continue;

    var num = fName[..2];
    var url = $"http://ikt.im/sk111p{num}";
    Console.WriteLine(url);
    
    var api = $"http://api.qrserver.com/v1/create-qr-code/?data={url}&size=500x500&ecc=H";

    var result = await client.GetByteArrayAsync(api);

    File.WriteAllBytes(png, result);
    await Task.Delay(500);
}

System.Console.WriteLine("done");

record Data(string Title, string Location, string Body, string Html);