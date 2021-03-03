using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Media;
//using Microsoft.Ink;//Color 存在しないらしい。
//using System.Drawing;//Color 存在しないらしい。

///<summary>
///単純落下
///左右移動
///BGM再生
///intを使うなら、Int32しか使わない。
///getLowerBound GetUpperBound Initialize();は使用禁止。
///b[,]を廃止することはできない。 右移動左移動 天上への移動、下移動 それぞれ4か所チェックするのは意外と手間がかかりそうだからだ。
///それよりもb[,]に転写して一括して 上下左右ひとつずらせるか確かめるほうが 効率が良い。
///いやそれでもあえて 廃止してみるか。廃止した。行数がぐっと減ったぞ！
///</summary>
namespace puyopuyo
{
    class Program
    {
        static void Main(string[] args)
        {
            //タイトル表示
            //ボード作成ゲーム開始

            //BGM再生 餅はモチ屋。BGMはBGM専門に取り扱うクラスにやってもらう。
            //RunRun bgm = new RunRun("abc.wav");
            //Thread t3= new Thread(new ThreadStart(bgm.saiseiBGM));
            //インスタンス
            Board brd = new Board();
            //初期設定 C#では クラス名と同じ名前のメソッドを、同じクラスの中にいれてはいけないらしい？
            brd.board();
            //ボード表示
            brd.copyNEXTToC();
            brd.createNewBlockonNEXT();
            Console.WriteLine("スレッド開始する");//スレッド操作が難しいという印象は見かけ倒しだった。
            Thread t1 = new Thread(new ThreadStart(brd.fallOff2secInterval));
            Thread t2 = new Thread(new ThreadStart(brd.kachakacha));
            t1.Start();
            t2.Start();
            //操作

        }
    }
    //あらゆるものは このBoardに組み込まれていなければならないはずだ。
    //get setは多次元配列を受け渡しできないのか？あ、そこでLINQを使うのか？
    //配列をごっそり引き渡せないんじゃあ、オブジェクト指向らしい記述ができそうにないよ。
    //C#の固有の便利な機能はまた今度試そう。
    class Board
    {
        /*System.Arrayクラスでは a[ay][ax] のように [ ] 角かっこを一つも書けないから、他次元配列はまるで利用できそうにないよ。
         * Arrayクラスではない通常の配列ならばarray2D[i,j] のように書くことになっている*/
        //Array a = Array.CreateInstance(typeof(Int32), 18, 10);//大きいほう 壁あり 確定ブロック積載されるほう
        //Array b = Array.CreateInstance(typeof(Int32), 16, 8);// ぷよぷよブロックを仮で置くだけのための配列
        //Array f = Array.CreateInstance(typeof(Int32),5,5);
        //int[,] array2D = new int[2, 3];
        Int32[,] a = new Int32[20, 16];
        Int32[,] c = new Int32[2, 2];//ぷよぷよブロックの形状 真
        Int32[,] d = new Int32[2, 2];
        Int32[,] cSwap = new Int32[2, 2];
        Int32[,] e = new Int32[20, 30];
        Int32[,] next = new Int32[2, 2];
        public Int32 correspondence;//何か所 ガシャンとぶつかってしまったかを数えるためのもの
        public String result; //何のresultだっけ。0や1などのパラーメーターで状況を指示するよりは文字列で伝えたほうがいい。
        private Int32 blockY = 5; //配列cではc[0,0]が 配列の中心で、そのcの中心中心が座標が配列 aにおいてどこにあるのかをblockXとblockYで表す。 
        private Int32 blockX = 5; //人間で例えるなら人間C君の中心は お腹だとしよう。配列aを地球としよう、人間C君の中心は地球のどこにあるのかを、北緯・南緯・東経・西経で表すこともできるだろう。「C君が地球のこの辺にいる。」という漠然とした指定の仕方は人間はできてもコンピュータではできない。C君の頭なのか、口なのか 胸なのか、腹なのか。C君の体のいったいどこが、北緯３５度、東経１３５度にあるのだろうか。人間にとってはそんな細かいことはどうだっていいんだけど、コンピュータはそうはいかない。今回は配列c[0,0]がa[,]のどこにあるのかをblockXとblockYで表すのだ。ちょっと難しかったかな。ミサイルを大木めがけて打つけど、大木の てっぺんなのか、根元なのか、幹なのか、大木のどこをうてばいいのか。
        private Int32 plusX = 0;
        private Int32 plusY = 0;
        private Int32 theColor;
        private Int32 count;
        private static Int32 timeInterval = 1550;
        private static Int32 altitude = 0;
        private static Int32 NOBLOCK = 0;
        private static Int32 WALL = 8;
        private static Int32 RED = 1;
        private static Int32 GREEN = 2;
        private static Int32 BLUE = 3;
        private static Int32 YELLOW = 4;
        private static Int32 PURPLE = 5;
        private static Int32 ENCUMBRANCE = 6;//お邪魔ブロック
        private static Int32 OPAQUE = 9;
        private Int32 total = 0;
        public void board()
        {
            correspondence = 0;
            result = "No";
            //aを初期設定
            refleshArray("a");
            refleshArray("b");
            refleshArray("c");
            refleshArray("cSwap");
            refleshArray("d");
            refleshArray("e");
            //フレームを作るんだ。
            setFrameA();

            //ブロック選択
            createNewBlockonNEXT();
            copyNEXTToC();
        }//ここまで コンストラクタ


        //---===turn===---
        //┌─┐   ┌─     ↑ 
        //↓     └─→  └─┘ 
        //左回転はばっちりだ テスト済み
        public void LeftKuruKuru()
        {
            //c→d   d[1-x,y]=c[y,x]; 内側合同
            //d[1,0]=c[0,0];
            //d[0,0]=c[0,1];
            //d[1,1]=c[1,0];
            //d[0,1]=c[1,1];
            for (Int32 cy = 0; cy < 2; cy++)
            {
                for (Int32 cx = 0; cx < 2; cx++)
                {
                    d[1 - cx, cy] = c[cy, cx];
                    cSwap[cy, cx] = c[cy, cx];
                }
            }
            Array.Copy(d, c, d.Length);

            //C→cSwap
            //Array.Copy(c, cSwap,c.Length);//Arrayは本当に多次元配列に対応しているのだろうか。


            //そのうえで本当に回転できるのか試す。
            //ブロックがぶつかっていなければtrueが返る。

            if (MergeCwithA("TURNRIGHT") != 0)
            {
                //cSwap→c
                Array.Copy(cSwap, c, cSwap.Length);//c.Lengthとは多次元配列の場合 いったいどの長さのことをいっているのだろう。
                Console.WriteLine("左回転あたはず。");
            }

        }
        //┌─┐  ┐ ↑
        //  ↓ ←┘ └─┘
        //二重for文を書けば一般化される。だが VisualC#は妙に記述量が多い。
        //たった4か所しか入れ替える場所がないのだから、手作業っぽくやるほうがいいのではないか。
        //右回転はばっちりだ。テスト済み。
        public void RightKuruKuru()
        {
            //c→d   d[b,1-a]=c[a,b]; 外側合同
            //d[0,1]=c[0,0];
            //d[1,1]=c[0,1];
            //d[0,0]=c[1,0];
            //d[1,0]=c[1,1];
            //c→d
            for (Int32 cy = 0; cy < 2; cy++)
            {
                for (Int32 cx = 0; cx < 2; cx++)
                {
                    d[cx, 1 - cy] = c[cy, cx];
                    cSwap[cy, cx] = c[cy, cx];
                }
            }
            //d→c
            Array.Copy(d, c, d.Length);

            //チェック
            if (MergeCwithA("TURNLEFT") != 0)
            {
                Console.WriteLine("右回転あたはず");
                //cSwap→c
                Array.Copy(cSwap, c, c.Length);
                //チェックしてだめだったんなら、すぐに着床処理しないと。
            }
        }


        //新規block生成 accomplished
        public void createNewBlockonNEXT()
        {
            //ランダム乱数
            Random rnd = new Random();
            //テトリスとはちょっとやり方が違う。
            next[0, 0] = 0;
            next[1, 0] = 0;
            next[0, 1] = rnd.Next(1, 5);
            next[1, 1] = rnd.Next(1, 5);
            //新しいblockをNEXTに置く
        }
        //accomplished
        public void copyNEXTToC()
        {
            Array.Copy(next, c, next.Length);
        }

        //GoldenFishUnkoNuriNuriFractal

        //recursive ある地点を開始地点にして 同じ 色のものが 何個連なっているのかを見る。
        //そこが 壁または ブロックなし、または半透明でなければ 半透明を塗る。
        public Int32 GoldenFishUnkoFractal()
        {
            count = 0;
            total = 0;
            for (Int32 ay = 0; ay < a.GetUpperBound(0); ay++)
            {
                for (Int32 ax = 0; ax < a.GetUpperBound(1); ax++)
                {
                    if (a[ay, ax] == RED || a[ay, ax] == GREEN || a[ay, ax] == BLUE || a[ay, ax] == YELLOW || a[ay, ax] == PURPLE)
                    {
                        //各要素を始点にして paintFractalを試みる。
                        theColor = a[ay, ax];//swap 安全な場所へ退避
                        //ここでようやく塗る
                        count = paintFractal(ay, ax, OPAQUE, theColor);
                        //塗った個数が1個以上3個以下なら 元に戻す。この記述はもう一度検証すべき。
                        if (4 <= count)
                        {
                            total += count;//塗った個数が1～3totalが
                        }
                        else
                        {
                            //3個未満ならやりなおしで 半透明だったところを元の色で塗りなおす。
                            paintFractal(ay, ax, theColor, OPAQUE);
                            count = 0;
                        }
                    }
                }
            }
            return total;
        }
        //半透明ブロックを NOBLOCK  ブロックがないようする。
        public void paintOpaqueToNoBlock()
        {
            for (Int32 ay = a.GetLowerBound(0); ay < a.GetUpperBound(0); ay++)
            {
                for (Int32 ax = a.GetLowerBound(1); ax < a.GetUpperBound(1); ax++)
                {
                    if (a[ay, ax] == OPAQUE)
                    {
                        a[ay, ax] = NOBLOCK;
                    }
                }
            }
        }
        //paintOpaqueToNoBlock()をやると隙間ができる。隙間を詰める。
        public void dropOff()
        {
            dropOffXDirection();
        }

        //これはprivateです。↑親の dropOffだけがpublicでほかのところから読み取れる。
        private void dropOffXDirection()
        {
            for (Int32 x = a.GetLowerBound(1); x < a.GetUpperBound(1); x++)
            {
                dropOffYDirection(a.GetLowerBound(0) + 1, x);
            }

        }
        //こっちが再帰関数であるために、↑ｘ方向の走査のためのメソッドが必要だった。
        //こちらはprivateです。↓ dropOffXdirectionの一部にすぎない。
        private void dropOffYDirection(Int32 dropY, Int32 dropX)
        {
            altitude = 0;
            if (dropY > 0)
            {
                if (a[dropY, dropX] != WALL && a[dropY, dropX] != NOBLOCK)
                {
                    for (altitude = dropY; altitude < a.GetUpperBound(0); altitude++)
                    {
                        if (a[altitude + 1, dropX] == NOBLOCK)
                        {
                            a[altitude + 1, dropX] = a[altitude, dropX];
                            a[altitude, dropX] = NOBLOCK;
                        }
                    }
                }
                dropOffYDirection(dropY - 1, dropX);
            }
        }

        //実際に消してもいいよっていう信号を塗る。
        //引数 py,pxは a[][]の座標、3番手colorは塗りたい色。最初に呼び出すときはOPAQUEを指定。
        //4番手 は 色を変えたい色覆い隠したい色 たとえば7つ黄色が連なってたら7つとも半透明にするやん。
        public Int32 paintFractal(Int32 py, Int32 px, Int32 color, Int32 beConceiled)
        {
            Int32 p_count = 1;
            if (a[py, px] != WALL && a[py, px] == beConceiled)//こっちは==color
            {
                if (a.GetLowerBound(0) < py && py <= a.GetUpperBound(0) && a.GetLowerBound(1) < px && px <= a.GetUpperBound(1))//引数に可笑しな値を入れてないか
                {
                    a[py, px] = color;

                    //↑NORTH
                    if (a[py - 1, px] != color && a[py - 1, px] != NOBLOCK && a[py - 1, px] == beConceiled)
                    {//こっちは!=color
                        p_count += paintFractal(py - 1, px, color, beConceiled);
                    }

                    //→EAST
                    if (a[py, px + 1] != color && a[py, px + 1] != NOBLOCK && a[py, px + 1] == beConceiled)
                    {
                        p_count += paintFractal(py, px + 1, color, beConceiled);
                    }

                    //↓SOUTH
                    if (a[py + 1, px] != color && a[py + 1, px] != NOBLOCK && a[py + 1, px] == beConceiled)
                    {
                        p_count += paintFractal(py + 1, px, color, beConceiled);
                    }

                    //←West ワイルドだろぅ～？
                    if (a[py, px - 1] != color && a[py, px - 1] != NOBLOCK && a[py, px - 1] == beConceiled)
                    {
                        p_count += paintFractal(py, px - 1, color, beConceiled);
                    }
                }
            }
            return p_count;
        }

        public void fallOff2secInterval()
        {
            do
            {
                Thread.Sleep(timeInterval);
                //Console.WriteLine("スレッドスリープ"+timeInterval);
                //Console.WriteLine("down");
                //Console.WriteLine("blockY=" + blockY + "blockX=" + blockX);
                if (MergeCwithA("DOWNWARD") == 0)
                {
                    blockY++;
                }
                else
                {
                    blockY = 4;
                    blockX = 4;
                }
                refleshArray("E");
                mergeACwithE();
                Console.Clear();
                Display("E");
            } while (true);
        }//public void 2secIntervalの終わり


        public void kachakacha()
        {
            //bool finish=false;
            ConsoleKeyInfo cki;//・・・・・・☆
            do
            {
                cki = Console.ReadKey(true);//・・・・・・☆

                if (cki.Key == ConsoleKey.LeftArrow)
                {
                    if (MergeCwithA("LEFTWARD") == 0)
                    {
                        blockX--;
                    }
                    Console.WriteLine("左移動 blockX=" + blockX);
                }
                if (cki.Key == ConsoleKey.RightArrow)
                {
                    if (MergeCwithA("RIGHTWARD") == 0)
                    {
                        blockX++;
                    }
                    Console.WriteLine("右移動 blockX=" + blockX);
                }
                if (cki.Key == ConsoleKey.DownArrow)
                {
                    //Display("c");
                    Console.WriteLine("右回転.");
                    RightKuruKuru();
                    //Display("c");
                }
                if (cki.Key == ConsoleKey.UpArrow)
                {
                    //Display("c");
                    Console.WriteLine("左回転.");
                    LeftKuruKuru();
                    //Display("c");
                }
                if (cki.Key == ConsoleKey.Spacebar)
                {
                    //Console.WriteLine("今すぐ○○を落とす！.");
                    //落としたのでblockの座標上のxとyの位置を初期位置に戻す
                    //↓ひとつ下へ移動する機能はこれで 十分。良い出来栄え。だが、左右移動がまだうまくいっていない。
                    if (0 == MergeCwithA("DOWNWARD"))
                    {
                        blockY++;
                    }
                    Console.WriteLine("下移動 blockX=" + blockY);
                }
                if (cki.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Escape.");
                    break;
                }
                Console.Clear();
                mergeACwithE();
                Display("E");
            } while (true /*cki.Key != ConsoleKey.Escape*/);//・・・・・・☆
            //escapeを押すと trueを返す。呼び出し側がfalseを受け取ったら呼び出し側の処理で このスレッドも含めてほかのスレッド全て終了させる。
        }

        void setFrameA()
        {
            for (Int32 ay = a.GetLowerBound(0); ay <= a.GetUpperBound(0); ay++)
            {
                for (Int32 ax = a.GetLowerBound(0); ax <= a.GetUpperBound(1); ax++)
                {
                    if (ay == a.GetLowerBound(0) ||
                        ay == a.GetLowerBound(0) + 1 ||
                        ay == a.GetUpperBound(0) ||
                        ay == a.GetUpperBound(0) - 1 ||
                        ax == a.GetLowerBound(1) ||
                        ax == a.GetLowerBound(1) + 1 ||
                        ax == a.GetUpperBound(1) ||
                        ax == a.GetUpperBound(1) - 1
                        )
                    {
                        a[ay, ax] = WALL;
                    }
                }
            }

        }
        //MergeCwithA(String Direction)は 論理演算で c[,]とa[,]がぶつかっていないかを確認するもので、実際にc[,]のデータをa[,]に書き込むことはしない。
        //ブロックがぶつかっているならfalse ブロックがぶつかっていないならtrueが返る
        //c[,]を直接a[,]に重ね合わせてみればいい。
        //これも完成した。
        public Int32 MergeCwithA(String Direction)
        {
            plusX = 0;//ここはテトリスとは違う。
            plusY = 0;//plusXとplusYは ブロックの中心座標より 一つ右か一つ左にもしも移動したら？を表す。
            correspondence = 0;//呼び出したらすぐに初期設定しないと。

            if (Direction.Equals("LEFTWARD"))
            {
                plusX = -1;
            }
            if (Direction.Equals("RIGHTWARD"))
            {
                plusX = 1;
            }
            if (Direction.Equals("DOWNWARD"))
            {
                plusY = 1;
            }
            //天井とぶつかっていないか
            if (Direction.Equals("UPWARD"))
            {
                plusY = -1;
            }
            if (Direction.Equals("TURNRIGHT"))
            {
                ;
            }
            if (Direction.Equals("TURNLEFT"))
            {
                ;
            }
            if (Direction.Equals("NEWTRAL"))
            {
                ;
            }
            //blockX blockYの存在を忘れてはいけない。
            /* 壁壁
             * 壁壁■□ ←blockX=2で、plusX=0のとき
             * 壁壁◎◎
             * 壁壁
             * 壁壁
             * 壁■□ ←blockX=2で、plusX=-1のとき
             * 壁◎◎
             * 壁壁
             * 壁壁    このようなときはさらに左へ移動させてはならない。
             * 壁壁      だが右への移動は できるようにしなければならない。
             * 壁壁
             * 壁壁    ■◎
             * 壁壁壁壁□◎壁壁壁壁
             * 壁壁壁壁壁壁壁壁壁壁
             * 
             * ■がブロックの中心座標
             * □が空白
             * ◎がぷよぷよブロック
             */
            //ブロックがぶつかっていない場合はtrueが返る
            //あ
            //なんとこれだけでいい。
            for (Int32 i = blockY + plusY, cy = 0; cy < 2; i++, cy++)
            {
                for (Int32 j = blockX + plusX, cx = 0; cx < 2; j++, cx++)
                {
                    Console.WriteLine("i=" + i + "j=" + j);
                    if ((a[i, j] != NOBLOCK) && (c[cy, cx] != NOBLOCK))
                    {
                        //ぶつかっているぞ！
                        correspondence++;
                        Console.WriteLine("a[" + i + "," + j + "]" + "でぶつかった。blockX=" + blockX + ",blockY=" + blockY);
                        break;
                    }
                }
                if (correspondence > 0)
                {
                    break;
                }
            }
            Console.WriteLine("correspondence=" + correspondence);
            //Display("A");//commitA処理の動作を確認するときに使う。
            return correspondence;
        }



        //↓テストはまだやっていない。
        public void mergeACwithE()
        {
            /*薄い折り紙の上に もう一枚薄い折り紙で覆うところを想像してみてほしい。
              1、e[][]をリフレッシュする。うまくいってる。
              2、a[][]をe[][]の上に重ね合わせる。うまくいってる
              3、c[][]をe[][]の上に重ね合わせる。
            */

            //１、
            refleshArray("E");


            //2、
            for (Int32 ay = 0, ey = 0; ay <= a.GetUpperBound(0); ay++, ey++)
            {
                for (Int32 ax = 0, ex = 0; ax <= a.GetUpperBound(1); ax++, ex++)
                {
                    e[ey, ex] = a[ay, ax];
                }
            }


            //3、宙に浮いている小さなものを貼り付けるならちょっと 頭をひねる必要があるよ。何枚か紙を用意して紙の上で書いて考えよう。
            for (Int32 i = blockY, cy = 0; cy <= c.GetUpperBound(0); i++, cy++)
            {
                for (Int32 j = blockX, cx = 0; cx <= c.GetUpperBound(1); j++, cx++)
                {
                    if (e[i, j] == NOBLOCK)
                    {
                        e[i, j] = c[cy, cx];
                    }
                }
            }

        }


        //テストやってない
        public void DisplayE()
        {

            for (Int32 ey = e.GetLowerBound(0); ey <= e.GetUpperBound(0); ey++)
            {
                for (Int32 ex = e.GetLowerBound(1); ex <= e.GetLowerBound(1); ex++)
                {
                    if (e[ey, ex] == NOBLOCK)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                        Console.Write("□");
                    }

                    if (e[ey, ex] == WALL)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("壁");
                    }

                    if (e[ey, ex] == RED)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("○");
                    }

                    if (e[ey, ex] == GREEN)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("△");
                    }

                    if (e[ey, ex] == BLUE)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("＠");
                    }

                    if (e[ey, ex] == YELLOW)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("◇");
                    }

                    if (e[ey, ex] == PURPLE)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("☆");
                    }

                    if (e[ey, ex] == ENCUMBRANCE)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("Θ");
                    }

                }
                Console.WriteLine();

            }
            Console.ResetColor();
            Console.WriteLine();

        }

        //テストやってない
        public bool commitA()
        {
            bool judge = true;
            for (Int32 ay = blockY, cy = 0; cy <= c.GetUpperBound(0); ay++, cy++)
            {
                for (Int32 ax = blockX, cx = 0; cx <= c.GetUpperBound(1); ax++, cx++)
                {
                    if (a[ay, ax] == NOBLOCK && c[cy, cx] != NOBLOCK)
                    {
                        a[ay, ax] = c[cy, cx];//aにブロックを描く
                    }
                    if (a[ay, ax] != NOBLOCK && c[cy, cx] != NOBLOCK)
                    {
                        judge = false;//wrong.
                    }
                }
            }

            return judge;
            //正常なときは0が戻される
            //異常が見つかれば、非０【ひ ゼロ】 が 戻される。
        }

        public void Display(String destination)
        {
            if (destination.Equals("A") || destination.Equals("a"))
            {
                for (Int32 ay = a.GetLowerBound(0); ay <= a.GetUpperBound(0); ay++)
                {
                    for (Int32 ax = a.GetLowerBound(1); ax <= a.GetUpperBound(1); ax++)
                    {
                        Console.Write(a[ay, ax]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();

            }

            if (destination.Equals("C") || destination.Equals("c"))
            {
                for (Int32 cy = c.GetLowerBound(0); cy <= c.GetUpperBound(0); cy++)
                {
                    for (Int32 cx = c.GetLowerBound(1); cx <= c.GetUpperBound(1); cx++)
                    {
                        Console.Write(c[cy, cx]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            if (destination.Equals("cSwap"))
            {
                for (Int32 csy = cSwap.GetLowerBound(0); csy <= cSwap.GetUpperBound(0); csy++)
                {
                    for (Int32 csx = cSwap.GetLowerBound(1); csx <= cSwap.GetUpperBound(1); csx++)
                    {
                        Console.Write(cSwap[csy, csx]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            if (destination.Equals("D") || destination.Equals("d"))
            {
                for (Int32 dy = d.GetLowerBound(0); dy <= d.GetUpperBound(0); dy++)
                {
                    for (Int32 dx = d.GetLowerBound(1); dx <= d.GetUpperBound(1); dx++)
                    {
                        Console.Write(d[dy, dx]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            if (destination.Equals("E") || destination.Equals("e"))
            {
                for (Int32 ey = e.GetLowerBound(0); ey <= e.GetUpperBound(0); ey++)
                {
                    for (Int32 ex = e.GetLowerBound(1); ex <= e.GetUpperBound(1); ex++)
                    {
                        Console.Write(e[ey, ex]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }
        //配列を確実に0で初期化します。NULLは代入しません。b.initialize()だとNULL値も混ぜて上書きするので、うまく回転できない。
        public void refleshArray(String destination)
        {
            //aは初期化したあと、すぐに枠を作るべきだ。
            if (destination.Equals("A") || destination.Equals("a"))
            {
                for (Int32 ay = a.GetLowerBound(0); ay < a.GetUpperBound(0); ay++)
                {
                    for (Int32 ax = a.GetLowerBound(1); ax < a.GetUpperBound(1); ax++)
                    {
                        a[ay, ax] = 0;
                    }

                }
                setFrameA();

            }

            if (destination.Equals("C") || destination.Equals("c"))
            {
                for (Int32 cy = c.GetLowerBound(0); cy <= c.GetUpperBound(0); cy++)
                {
                    for (Int32 cx = c.GetLowerBound(1); cx <= c.GetUpperBound(1); cx++)
                    {
                        c[cy, cx] = 0;
                    }
                }
            }
            if (destination.Equals("cSwap"))
            {
                for (Int32 csy = cSwap.GetLowerBound(0); csy <= cSwap.GetUpperBound(0); csy++)
                {
                    for (Int32 csx = cSwap.GetLowerBound(1); csx <= cSwap.GetUpperBound(1); csx++)
                    {
                        cSwap[csy, csx] = 0;
                    }
                }
            }
            if (destination.Equals("D") || destination.Equals("d"))
            {
                for (Int32 dy = d.GetLowerBound(0); dy <= d.GetUpperBound(0); dy++)
                {
                    for (Int32 dx = d.GetLowerBound(1); dx <= d.GetUpperBound(1); dx++)
                    {
                        d[dy, dx] = 0;
                    }
                }
            }
            if (destination.Equals("E") || destination.Equals("e"))
            {
                for (Int32 ey = e.GetLowerBound(0); ey < e.GetUpperBound(0); ey++)
                {
                    for (Int32 ex = e.GetLowerBound(1); ex < e.GetUpperBound(1); ex++)
                    {
                        e[ey, ex] = 0;
                    }
                }
            }
        }

        //このクラスの終わり
    }
    class RunRun
    {
        private SoundPlayer player;

        public RunRun(String location)
        {
            player = new SoundPlayer();//これが出発点。
            player.SoundLocation = location;//ファイルの場所指定   
        }

        public void saiseiBGM()
        {
            try //consoleアプリケーションだからこそ積極的にtry{}catch{}を使えばいい
            {
                player.LoadAsync();
                player.Play();
            }
            catch
            {
                System.Console.WriteLine("I cannot play a wav file.");
                Console.ReadKey();
            }
        }
    }
    class JanJan
    {
        private SoundPlayer player;
        private String effectSoundLocation;
        public JanJan(Int32 rensa)
        {
            effectSoundLocation = "1HIT.wav";
            player = new SoundPlayer();//これが出発点。
            player.SoundLocation = effectSoundLocation;//ファイルの場所指定   
        }

        public void saiseiBGM()
        {
            try //consoleアプリケーションだからこそ積極的にtry{}catch{}を使えばいい
            {
                player.LoadAsync();
                player.Play();
            }
            catch
            {
                System.Console.WriteLine("I cannot play a wav file.");
                //Console.ReadKey();
            }
        }
    }
}// namespaceの終わり
