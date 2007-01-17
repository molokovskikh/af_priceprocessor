<?xml version='1.0' encoding = "windows-1251" standalone = "yes"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output method="xml" indent="yes" encoding="windows-1251" />

	<xsl:template match="/" >
		<Price>

			<xsl:for-each select=" оммерческа€»нформаци€/ѕакетѕредложений/ѕредложени€/ѕредложение">
				<Position>
					<Code>
						<xsl:value-of select="»д"/>
					</Code>
					<Name1>
						<xsl:value-of select="Ќаименование"/>
					</Name1>
					<FirmCr>
						<xsl:value-of select="»зготовитель/ќфициальноеЌаименование"/>
					</FirmCr>
					<Period>
						<xsl:value-of select="«начени€—войств/«начени€—войства[child::»д = 'EXP_DATE']/«начение"/>
					</Period>
					<Volume>
						<xsl:value-of select="«начени€—войств/«начени€—войства[child::»д = 'SPACK']/«начение"/>
					</Volume>
					<Quantity>
						<xsl:value-of select="«начени€—войств/«начени€—войства[child::»д = 'QTY']/«начение"/>
					</Quantity>
					<xsl:for-each select="÷ены/÷ена">
						<xsl:element name="Cost{»д“ипа÷ены}">
							<xsl:value-of select="÷ена«а≈диницу"/>
						</xsl:element>
					</xsl:for-each>

					</Position>
			</xsl:for-each>

		</Price>
	</xsl:template>

</xsl:stylesheet>
