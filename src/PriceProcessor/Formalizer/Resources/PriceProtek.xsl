<?xml version='1.0' encoding = "windows-1251" standalone = "yes"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output method="xml" indent="yes" encoding="windows-1251" />

	<xsl:template match="/" >
		<xsl:variable name="CodeCr" select="КоммерческаяИнформация/ПакетПредложений/Ид"/>
		
		<Price>

			<xsl:for-each select="КоммерческаяИнформация/ПакетПредложений/Предложения/Предложение">
				<Position>
					<Code>
						<xsl:value-of select="Ид"/>
					</Code>
					<CodeCr>
						<xsl:value-of select="$CodeCr"/>
					</CodeCr>
					<Name1>
						<xsl:value-of select="Наименование"/>
					</Name1>
					<FirmCr>
						<xsl:value-of select="Изготовитель/ОфициальноеНаименование"/>
					</FirmCr>
					<Period>
						<xsl:value-of select="ЗначенияСвойств/ЗначенияСвойства[child::Ид = 'EXP_DATE']/Значение"/>
					</Period>
					<Volume>
						<xsl:value-of select="ЗначенияСвойств/ЗначенияСвойства[child::Ид = 'SPACK']/Значение"/>
					</Volume>
					<Quantity>
						<xsl:value-of select="ЗначенияСвойств/ЗначенияСвойства[child::Ид = 'QTY']/Значение"/>
					</Quantity>
					<xsl:for-each select="Цены/Цена">
						<xsl:element name="Cost{ИдТипаЦены}">
							<xsl:value-of select="ЦенаЗаЕдиницу"/>
						</xsl:element>
					</xsl:for-each>

					</Position>
			</xsl:for-each>

		</Price>
	</xsl:template>

</xsl:stylesheet>
