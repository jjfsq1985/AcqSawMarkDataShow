EXTERN_C DLLSAWMARK_API bool polyFit(const double DataIn[], const int nDataLen, int nPolyN, double dblCoefOut[])
{
	if (dblCoefOut == NULL || nPolyN <= 0 || nPolyN >= 6)
		return false;
	czy::Fit *pfit = new czy::Fit();
	double *pDataInX = new double[nDataLen];
	for (int i = 0; i < nDataLen; i++)
		pDataInX[i] = i;
	pfit->polyfit(pDataInX, DataIn, nDataLen, nPolyN, false);
	int nCount = pfit->getFactorSize();
	if (nCount != nPolyN + 1)
		return false;
	for (int i = 0; i <= nPolyN; i++)
		dblCoefOut[i] = pfit->getFactor(i);
	return true;
}

EXTERN_C DLLSAWMARK_API double polyFitGetY(double dbCoef[], int nCoefCount, double dblX)
{
	double ans = 0.0;
	for (size_t i = 0; i < nCoefCount; ++i)
	{
		ans += dbCoef[i] * pow(dblX, (int)i);
	}
	return ans;
}