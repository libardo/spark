
#include "stdafx.h"
#include "Language.h"
#include "CodeWindowManager.h"
#include "Colorizer.h"
#include "Source.h"
#include "ColorableItem.h"


STDMETHODIMP Language::GetSource(IVsTextBuffer* pBuffer, ISparkSource** ppSource)
{
	HRESULT hr = S_OK;

	CComCritSecLock<CComCriticalSection> lock(_sourcesLock);

	CComPtr<IUnknown> key;
	_HR(pBuffer->QueryInterface(&key));

	int index = _sources.FindKey(key);

	if (index == -1)
	{
		CComPtr<ISparkSource> source;
		
		SourceInit init = {_site};
		_HR(pBuffer->QueryInterface(&init._primaryBuffer));
		_HR(Source::CreateInstance(init, &source));
		
		_HR(_supervisor->OnSourceAssociated(source));

		_HR(source->QueryInterface(ppSource));

		if (SUCCEEDED(hr))
			_sources.Add(key.Detach(), source.Detach());
	}
	else
	{
		_sources.GetValueAt(index)->QueryInterface(ppSource);
	}
	return hr;
}

STDMETHODIMP Language::GetColorizer( 
    /* [in] */ __RPC__in_opt IVsTextLines *pBuffer,
    /* [out] */ __RPC__deref_out_opt IVsColorizer **ppColorizer)
{
	HRESULT hr = S_OK;

	CComPtr<IVsProvideColorableItems> csharpItems;
	_HR(_site->QueryService(__uuidof(CSharp), &csharpItems));
	int csharpItemCount;
	_HR(csharpItems->GetItemCount(&csharpItemCount));

	ColorizerInit init = {this, pBuffer, csharpItemCount + 1};
	_HR(Colorizer::CreateInstance(init, ppColorizer));
	return hr;
}

STDMETHODIMP Language::GetCodeWindowManager( 
    /* [in] */ __RPC__in_opt IVsCodeWindow *pCodeWin,
    /* [out] */ __RPC__deref_out_opt IVsCodeWindowManager **ppCodeWinMgr)
{
	HRESULT hr = S_OK;
	CodeWindowManagerInit init = {this, pCodeWin};
	_HR(CodeWindowManager::CreateInstance(init, ppCodeWinMgr));
	return hr;
}

CTraceCategory sparkLanguage(_T("Spark Language"), 1);

STDMETHODIMP Language::GetItemCount( 
    /* [out] */ __RPC__out int *piCount)
{
#ifdef _DEBUG
	sparkLanguage.SetStatus(ATLTRACESTATUS::ATLTRACESTATUS_ENABLED);
#endif

	HRESULT hr = S_OK;

	CComPtr<IVsProvideColorableItems> csharpItems;
	_HR(_site->QueryService(__uuidof(CSharp), &csharpItems));
	int csharpItemCount;
	_HR(csharpItems->GetItemCount(&csharpItemCount));

	CComPtr<IVsProvideColorableItems> sparkItems;
	_HR(_supervisor->QueryInterface(&sparkItems));
	int sparkItemCount;
	_HR(sparkItems->GetItemCount(&sparkItemCount));
	
	*piCount = csharpItemCount + sparkItemCount;
	ATLTRACE(sparkLanguage, 1, _T("GetItemCount %d\n"), *piCount);
	return hr;
}

STDMETHODIMP Language::GetColorableItem( 
    /* [in] */ int iIndex,
    /* [out] */ __RPC__deref_out_opt IVsColorableItem **ppItem)
{
	ATLTRACE(sparkLanguage, 1, _T("GetColorableItem %d\n"), iIndex);

	HRESULT hr = S_OK;

	// return csharp color info for lower band
	CComPtr<IVsProvideColorableItems> csharpItems;
	_HR(_site->QueryService(__uuidof(CSharp), &csharpItems));

	int csharpItemCount;
	_HR(csharpItems->GetItemCount(&csharpItemCount));

	if (SUCCEEDED(hr) && iIndex <= csharpItemCount)
	{
		_HR(csharpItems->GetColorableItem(iIndex, ppItem));
		
		CComBSTR name;
		_HR((*ppItem)->GetDisplayName(&name));
		if (SUCCEEDED(hr))
			ATLTRACE(sparkLanguage, 1, L"Color name %s\n", name);

		return hr;
	}

	// return spark color info for upper band
	CComPtr<IVsProvideColorableItems> sparkItems;
	_HR(_supervisor->QueryInterface(&sparkItems));

	int sparkItemCount;
	_HR(sparkItems->GetItemCount(&sparkItemCount));

	if (SUCCEEDED(hr) && iIndex <= csharpItemCount + sparkItemCount)
	{
		_HR(sparkItems->GetColorableItem(iIndex - csharpItemCount + 1, ppItem));

		CComBSTR name;
		_HR((*ppItem)->GetDisplayName(&name));
		if (SUCCEEDED(hr))
			ATLTRACE(sparkLanguage, 1, L"Color name %s\n", name);

		return hr;
	}
	return E_INVALIDARG;
}
