import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { Header } from 'components/Header/Header'
import { createContext, useEffect, useState } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage } from './FrontPage'
import { MissionPage } from './MissionPage'

export const AccessTokenContext = createContext('')

export function FlotillaSite() {
    const authContext = useMsal()
    const [accessToken, setAccessToken] = useState('')
    useEffect(() => {
        fetchAccessToken(authContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
    }, [])
    return (
        <>
            {accessToken === '' && <>Loading...</>}
            {accessToken !== '' && (
                <>
                    <AccessTokenContext.Provider value={accessToken}>
                        <Header/>
                        <BrowserRouter>
                            <Routes>
                                <Route path="/" element={<FrontPage />} />
                                <Route path="/mission" element={<MissionPage />} />
                            </Routes>
                        </BrowserRouter>
                    </AccessTokenContext.Provider>
                </>
            )}
        </>
    )
}