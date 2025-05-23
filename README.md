# 🛠️ Ducktastic Unity C# IDE

Ducktastic Unity C# IDE, Unity projeleri için özelleştirilmiş, JetBrains tarzında koyu temalı, AvalonEdit tabanlı ve modern WPF arayüzüne sahip bir C# düzenleyicisidir. Unity ile çalışan geliştiriciler için basit, etkili ve entegre bir düzenleme ortamı sağlar.

## 🚀 Özellikler

- 🎨 Koyu tema & JetBrains Mono yazı tipi
- 📁 Dosya tarayıcı: Proje klasör yapısını gösterir
- 📑 Sekmeli dosya düzenleme sistemi
- 🧠 Kod tamamlama: Temel C# anahtar kelimeleri
- 🧱 UnityEngine & UnityEditor.dll desteği
- 🔧 Derleme: Kodları anında derler, hataları gösterir
- 🖥️ Console: `Console.WriteLine` çıktısını gösterir
- ✅ Hata paneli ve çıktı paneli ayrı ayrı görüntülenir
- 📄 Yeni dosya oluşturma: Kayıt konumu ve adını seçtirir
- 🧰 AvalonEdit tabanlı gelişmiş metin editörü

## 🖼️ Görünüm

![Ekran Görüntüsü](docs/screenshot.png)

## 🧑‍💻 Kurulum

1. Bu projeyi klonlayın:
   ```bash
   git clone https://github.com/kullaniciadi/UnityIDE.git

2.Visual Studio 2022 ile açın.

3. Gerekli NuGet bağımlılıklarını yükleyin:

 - AvalonEdit

 - ScintillaNET (şimdilik opsiyonel)

 - Microsoft.CodeDom.Providers.DotNetCompilerPlatform

 - MainWindow.xaml içinde Unity Editor yolunu kendinize göre ayarlayın:
   ```bash
   string unityDllPath = @"C:\\Program Files\\Unity\\Hub\\Editor\\2021.3.0f1\\Editor\\Data\\Managed\\UnityEngine";

## 📦Derleme
 - Ctrl + Shift + B ya da “Derle” menüsünü kullanarak uygulamayı çalıştırabilirsiniz.

 - Dosya Aç ya da Yeni Dosya ile düzenleme yapabilirsiniz.

## 🧩 Geliştirici Notları
Derleme işlemi CSharpCodeProvider ile yapılır, çıktılar TextBox içinde gösterilir.
Unity .dll referansları manuel olarak eklenmiştir. Unity ile tam uyumlu hale getirmek için versiyon uyumuna dikkat edilmelidir.

📄 Lisans
Bu proje MIT lisansı ile lisanslanmıştır. Detaylar için LICENSE dosyasına göz atın.

## Geliştirici: Ducktastic Studio
Unity için özelleştirilmiş hafif C# IDE deneyimi sunar.
