using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphComponent
{
    public class LeastSquare
    {
        private double[] factor = null; ///<拟合后的方程系数
		private  double ssr = 0.0;                 ///<回归平方和
		private  double sse = 0.0;                 ///<(剩余平方和)
		private  double rmse = 0.0;                ///<RMSE均方根误差
	public LeastSquare()
    {
        factor = new double[2];
    }

		///
		/// \brief 直线拟合-一元回归,拟合的结果可以使用getFactor获取，或者使用getSlope获取斜率，getIntercept获取截距
		/// \param x 观察值的x
		/// \param y 观察值的y
		///
        public bool linearFit(double[] x, double[] y)
        {
            int nLen = getSeriesLength(x,y);
            return linearFit(x,y,nLen);
        }
		public bool linearFit(double[] x, double[] y,int nlength)
		{
            factor = new double[2];
			double t1=0, t2=0, t3=0, t4=0;
			for(int i=0; i<nlength; ++i)
			{
				t1 += x[i]*x[i];
				t2 += x[i];
				t3 += x[i]*y[i];
				t4 += y[i];
			}
			factor[1] = (t3*nlength - t2*t4) / (t1*nlength - t2*t2);
			factor[0] = (t1*t4 - t2*t3) / (t1*nlength - t2*t2);
			//////////////////////////////////////////////////////////////////////////
			//计算误差
			calcError(x,y,nlength,ref ssr,ref sse,ref rmse);
			return true;
		}
		///
		/// \brief 多项式拟合，拟合y=a0+a1*x+a2*x^2+……+apoly_n*x^poly_n
		/// \param x 观察值的x
		/// \param y 观察值的y
		/// \param poly_n 期望拟合的阶数，若poly_n=2，则y=a0+a1*x+a2*x^2
		public void polyfit(double[] x,double[] y,int poly_n)
		{
			polyfit(x,y,getSeriesLength(x,y),poly_n);
		}

		public void polyfit(double[] x,double[] y,int nLen,int poly_n)
		{
			factor = new double[poly_n+1];
			int i,j;
			
            double[] tempx = new double[nLen];
            double[] tempy = new double[nLen];
            for(i=0; i< nLen; i++)
            {
                tempx[i] = 1.0;
                tempy[i] = y[i];
            }

            double[] sumxx = new double[poly_n*2+1];
            double[] ata = new double[(poly_n+1)*(poly_n+1)];
            double[] sumxy = new double[poly_n+1];

			for (i=0;i<2*poly_n+1;i++)
            {
				for (sumxx[i]=0,j=0;j<nLen;j++)
				{
					sumxx[i]+=tempx[j];
					tempx[j]*=x[j];
				}
			}
			for (i=0;i<poly_n+1;i++)
            {
				for (sumxy[i]=0,j=0;j<nLen;j++)
				{
					sumxy[i]+=tempy[j];
					tempy[j]*=x[j];
				}
			}
			for (i=0;i<poly_n+1;i++)
				for (j=0;j<poly_n+1;j++)
					ata[i*(poly_n+1)+j]=sumxx[i+j];
			gauss_solve(poly_n+1,ata,factor,sumxy);
			//计算误差
			calcError(x,y,nLen,ref ssr,ref sse,ref rmse);

		}
		/// 
		/// \brief 获取系数
		/// \param 存放系数的数组
		///
		public void getFactor(ref double[] dbfactorOut)
        {
            dbfactorOut = factor;
        }

		/// 
		/// \brief 根据x获取拟合方程的y值
		/// \return 返回x对应的y值
		///
		public double getY(double x)
		{
			double ans = 0.0;
			for (int i=0;i<factor.Length;++i)
			{
				ans += factor[i]*Math.Pow(x,i);
			}
			return ans;
		}
		/// 
		/// \brief 获取斜率
		/// \return 斜率值
		///
		public double getSlope()
        {
            return factor[1];
        }
		/// 
		/// \brief 获取截距
		/// \return 截距值
		///
		public double getIntercept()
        {
            return factor[0];
        }
		/// 
		/// \brief 剩余平方和
		/// \return 剩余平方和
		///
		public double getSSE()
        {
            return sse;
        }
		/// 
		/// \brief 回归平方和
		/// \return 回归平方和
		///
		public double getSSR()
        {
            return ssr;
        }
		/// 
		/// \brief 均方根误差
		/// \return 均方根误差
		///
		public double getRMSE()
        {
            return rmse;
        }
		/// 
		/// \brief 确定系数，系数是0~1之间的数，是数理上判定拟合优度的一个量
		/// \return 确定系数
		///
		public double getR_square()
        {
            return 1-(sse/(ssr+sse));
        }
		/// 
		/// \brief 获取两个vector的安全size
		/// \return 最小的一个长度
		///
		private int getSeriesLength(double[] x,double[] y)
		{
			return (x.Length > y.Length ? y.Length : x.Length);
		}
		/// 
		/// \brief 计算均值
		/// \return 均值
		private static double Mean(double[] v)
		{
			double total = 0.0;
			for (int i=0;i<v.Length;++i)
			{
				total += v[i];
			}
			return (total / v.Length);
		}
		/// 
		/// \brief 获取拟合方程系数的个数
		/// \return 拟合方程系数的个数
		///
		public int getFactorSize()
        {
            return factor.Length;
        }
		/// 
		/// \brief 根据阶次获取拟合方程的系数，
		/// 如getFactor(2),就是获取y=a0+a1*x+a2*x^2+……+apoly_n*x^poly_n中a2的值
		/// \return 拟合方程的系数
		///
		public double getFactor(int i)
        {
            return factor[i];
        }
		private void calcError(double[] x,double[] y,int nLen, ref double r_ssr, ref double r_sse, ref double r_rmse)
		{
			double mean_y = Mean(y);
			double yi = 0.0;
			for (int i=0; i<nLen; ++i)
			{
				yi = getY(x[i]);
				r_ssr += ((yi-mean_y)*(yi-mean_y));//计算回归平方和
				r_sse += ((yi-y[i])*(yi-y[i]));//残差平方和
			}
			r_rmse =  Math.Sqrt(r_sse/nLen);
		}

		private void gauss_solve(int n ,double[] A, double[] x ,double[] b)
		{
			int i,j,k,r;
			double max;
			for (k=0;k<n-1;k++)
			{
				max= Math.Abs(A[k*n+k]); /*find maxmum*/
				r=k;
				for (i=k+1;i<n-1;i++)
                {
                    if (max < Math.Abs(A[i * n + i]))
					{
                        max = Math.Abs(A[i * n + i]);
						r=i;
					}
				}
				if (r!=k)
                {
					for (i=0;i<n;i++)         /*change array:A[k]&A[r] */
					{
						max=A[k*n+i];
						A[k*n+i]=A[r*n+i];
						A[r*n+i]=max;
					}
				}
				max=b[k];                    /*change array:b[k]&b[r]     */
				b[k]=b[r];
				b[r]=max;
				for (i=k+1;i<n;i++)
				{
                    for (j = k + 1; j < n; j++)
                    {
                        A[i * n + j] -= A[i * n + k] * A[k * n + j] / A[k * n + k];
                    }
					b[i]-=A[i*n+k]*b[k]/A[k*n+k];
				}
			}

            for (i = n - 1; i >= 0; x[i] /= A[i * n + i], i--)
            {
                for (j = i + 1, x[i] = b[i]; j < n; j++)
                {
                    x[i] -= A[i * n + j] * x[j];
                }
            }
		}
    }
}
