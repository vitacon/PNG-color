using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace PNG_color
{
    class Program
    {
        static int Main(string[] args)
        {
            /*
             * Return
             * 0    ok
             * -1   no args
             * -2   incompatible resolutions
             * -3   file not found
             */

            Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            Console.WriteLine("PNG Colorizer v" + version.Major + "." + version.Minor + "\n");

            long msstart = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            String filnam1 = "";
            String filnam2 = "";
            String filout = "";
            bool b_wait = false;
            bool b_resize = false;
            int bytePerPixel = 4; // natvrdo!
            int zoom = 1;
            int result = 0;

            //int percentage = 100;   // kolik procent saturace masky se použije
            double fpercentage = 1;        // normalizovaná saturace jako float

            if (args.Length < 3)
            {
                Console.WriteLine("Usage: PNG-color grayscaleimage colormaskimage outputimage [saturation percentage] [RW]\n\n");
                Console.WriteLine("       R   force resing");
                Console.WriteLine("       W   waits for key");
                Console.WriteLine("       Replacing grayscaleimage with @ normalizes luminance.\n");
                return -1;
            }
            else
            {
                filnam1 = args[0];
                filnam2 = args[1];
                filout = args[2];

                if (args.Length > 3)
                {
                    // třetí parametr může být percentage
                    int number = 0;
                    try
                    {
                        number = Convert.ToInt32(args[3]);
                    }
                    catch (FormatException)
                    {
                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("The number cannot fit in an Int32.");
                    }
                    finally
                    {
                        if (number > 0 && number < 1000)
                        {
                            //percentage =number;
                            fpercentage = (double)number / 100.0;
                        }
                    }

                    // dodatečné parametry na konci
                    string addarg = (args[args.Length - 1]).ToUpper();
                    if (addarg.Contains("W"))
                        b_wait = true;
                    if (addarg.Contains("R"))
                        b_resize = true;
                }
            }

            Console.WriteLine("Grayscale:\t" + filnam1);
            Console.WriteLine("Colormask:\t" + filnam2);
            Console.WriteLine("Output:   \t" + filout);
            Console.WriteLine("Saturation:\t" + Math.Round(fpercentage * 100) + " %");
            Console.WriteLine("Force size:\t" + b_resize);


            try
            {
                // načtu pracovní obrazky
                Bitmap bitmap1;

                Bitmap bitmap2 = new Bitmap(filnam2);

                int xsize2 = bitmap2.Width;
                int ysize2 = bitmap2.Height;

                if (filnam1 == "@")
                {
                    // normalizovaná luminance
                    bitmap1 = new Bitmap(xsize2, ysize2, bitmap2.PixelFormat);
                    Graphics g = Graphics.FromImage(bitmap1);
                    g.Clear(Color.Gray);
                    g.Dispose();
                }
                else
                {
                    // normální soubor
                    bitmap1 = new Bitmap(filnam1);
                }

                int xsize = bitmap1.Width;
                int ysize = bitmap1.Height;


                Console.WriteLine("Resolution 1:\t" + xsize + " x " + ysize);

                // pokud se rozměry liší, tak zkusíme nastavit zoom
                if (xsize != xsize2 || ysize != ysize2)
                {
                    Console.WriteLine("Resolution 2:\t" + xsize2 + " x " + ysize2);

                    // Resize masky
                    if (b_resize)
                    {
                        // vygenerujeme nový bitmap masky ve velikosti čb
                        // a nejlépe s bikubickou interpolací
                        Bitmap bitmap_r = new Bitmap(xsize, ysize);

                        Graphics g = Graphics.FromImage((System.Drawing.Image)bitmap_r);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        // Draw image with new width and height  
                        g.DrawImage(bitmap2, 0, 0, xsize, ysize);
                        g.Dispose();

                        // aktualizujeme rozměr
                        bitmap2 = bitmap_r;
                        bitmap_r = null;
                        xsize2 = xsize;
                        ysize2 = ysize;

                        Console.WriteLine("Resized to:\t" + xsize + " x " + ysize);
                    }

                    zoom = xsize / xsize2;
                    // souhlasí zoom pro oba rozměry?
                    if (xsize2 * zoom != xsize || ysize2 * zoom != ysize)
                    {
                        // rozměry nesedí, takže končíme
                        Console.WriteLine("\nThe resolutions are not compatible!\nUse the switch R to resize the color mask.");
                        result = -2;

                    }
                    else
                    {
                        // jo, můžeme pokračovat
                        Console.WriteLine("Zoom:\t\t" + zoom);
                    }
                }
                Console.WriteLine();

                if (result == 0)
                {
                    Rectangle rect = new Rectangle(0, 0, xsize, ysize);
                    Rectangle rect2 = new Rectangle(0, 0, xsize2, ysize2);

                    // bitmap3 bude výstup
                    Bitmap bitmap3 = new Bitmap(xsize, ysize, bitmap1.PixelFormat);

                    // barevné operace
                    int x, y;

                    unsafe
                    {
                        BitmapData bData1 = bitmap1.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        BitmapData bData2 = bitmap2.LockBits(rect2, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        BitmapData bData3 = bitmap3.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                        byte* pixPointerGray = (byte*)bData1.Scan0;
                        byte* pixPointerCol = (byte*)bData2.Scan0;
                        byte* pixPointerMix = (byte*)bData3.Scan0;

                        for (y = 0; y < ysize; y++)
                        {
                            for (x = 0; x < xsize; x++)
                            {
                                //přímý přístup
                                /*
                                pixPointer3[2] = pixPointer1[2];
                                pixPointer3[1] = pixPointer1[2];
                                pixPointer3[0] = 255;
                                */

                                // pro lepší manévrování si vytáhneme RGB barevné masky do proměnných
                                double dr = pixPointerCol[2];
                                double dg = pixPointerCol[1];
                                double db = pixPointerCol[0];

                                double lum = 0.3 * dr + 0.59 * dg + 0.11 * db;

                                // pokud je lum přiliš malá (pod lumcomplimit), tak bude saturace barvy příliš výrazná a bylo by vhodné ji trochu ztlumit
                                const double lumcomplimit = 10d;
                                if (lum < lumcomplimit)
                                {
                                    //Debug.WriteLine(dr + " " + dg + " " + db);
                                    double compensation = lumcomplimit - lum;
                                    // čím je barva tmavší, tím víc ji posuneme blíže k šedé
                                    dr += compensation;
                                    dg += compensation;
                                    db += compensation;
                                    lum = 0.3 * dr + 0.59 * dg + 0.11 * db;
                                }

                                // pokud je perc jiné než 1, tak upravíme saturaci
                                if (fpercentage != 1)
                                {
                                    // jednotlivé složky se budou blížit nebo vzdalovat hodnotě lum
                                    dr = (dr * fpercentage) + lum * (1 - fpercentage);
                                    dg = (dg * fpercentage) + lum * (1 - fpercentage);
                                    db = (db * fpercentage) + lum * (1 - fpercentage);

                                    // pro jistotu přepočítáme lum (teoreticky by se neměla lišit, ne?)
                                    lum = 0.3 * dr + 0.59 * dg + 0.11 * db;
                                }

                                // světlost šedého pixelu
                                double gray = 0.3 * pixPointerGray[2] + 0.59 * pixPointerGray[1] + 0.11 * pixPointerGray[0];

                                if (lum == 255)
                                {
                                    pixPointerMix[2] = pixPointerGray[2];
                                    pixPointerMix[1] = pixPointerGray[1];
                                    pixPointerMix[0] = pixPointerGray[0];
                                }
                                else
                                {
                                    // všechno potřebuju mít v rozsahu 0-1
                                    lum = lum / 255;
                                    gray = gray / 255;
                                    dr /= 255;
                                    dg /= 255;
                                    db /= 255;
                                    double ar, ag, ab;
                                    //
                                    // brutální korekce na příliš bílou barevnou masku
                                    double lumlimit = 0.75;
                                    if (lum > lumlimit)
                                    {
                                        double correction = lumlimit / lum;
                                        lum = lumlimit;
                                        dr *= correction;
                                        dg *= correction;
                                        db *= correction;
                                    }

                                    // upravíme barevné složky tak, aby jejich světlost odpovídala světlosti šedého pixelu

                                    // R
                                    ar = (dr - lum) / lum / (lum - 1);
                                    dr = (ar * gray * gray) + ((1.0 - ar) * gray);
                                    // G
                                    ag = (dg - lum) / lum / (lum - 1);
                                    dg = (ag * gray * gray) + ((1.0 - ag) * gray);
                                    // B
                                    ab = (db - lum) / lum / (lum - 1);
                                    db = (ab * gray * gray) + ((1.0 - ab) * gray);
                                    //
                                    int r = (int)(dr * 255);
                                    int g = (int)(dg * 255);
                                    int b = (int)(db * 255);
                                    // opravy přetečení
                                    if (r > 255)
                                        r = 255;
                                    if (r < 0)
                                        r = 0;
                                    if (g > 255)
                                        g = 255;
                                    if (g < 0)
                                        g = 0;
                                    if (b > 255)
                                        b = 255;
                                    if (b < 0)
                                        b = 0;
                                    //
                                    pixPointerMix[3] = (byte)255;
                                    pixPointerMix[2] = (byte)r;
                                    pixPointerMix[1] = (byte)g;
                                    pixPointerMix[0] = (byte)b;
                                }

                                // všechny pointery posuneme o pixel
                                pixPointerGray += bytePerPixel;
                                pixPointerMix += bytePerPixel;
                                // v výjimkou barvy, která se posouvá v závislosti na zoomu
                                if ((x % zoom) == (zoom - 1))
                                    pixPointerCol += bytePerPixel;
                            }
                            // a na konci lichého řádku se barevný pointer posune zpátky na začátek toho svého
                            //*** 2022: Tohle je nějaké nedomyšlené, ne? Co když je zoom 4? A co takhle místo ysize raději používat bData2.Stride?
                            if (y % zoom == 1) // bylo 0, ale to blblo při zoom == 1
                                pixPointerCol = (byte*)bData2.Scan0 + (y / 2) * xsize2 * bytePerPixel;
                            // tečkování na obrazovce
                            if ((y & 127) == 127) Console.Write(".");
                        }

                        // odemkneme obrázky
                        bitmap3.UnlockBits(bData3);
                        bitmap2.UnlockBits(bData2);
                        bitmap1.UnlockBits(bData1);
                    }
                    // uložím výsledek
                    bitmap3.Save(filout, System.Drawing.Imaging.ImageFormat.Png);

                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Error opening file.\n\n" + e);
                result = -3;
            }

            long msend = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            Console.WriteLine("\n\nDuration:\t" + ((double)(msend - msstart)) / 1000 + " s");

            Console.WriteLine("\nDone.");
            if (b_wait) Console.ReadKey();

            return result;
        }
    }
}
